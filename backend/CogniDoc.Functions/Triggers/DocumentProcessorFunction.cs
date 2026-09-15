using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Azure.Storage.Blobs;
using CogniDoc.Application.Models;
using CogniDoc.Functions.Configuration;
using CogniDoc.Functions.Extensions.Infrastructure;
using CogniDoc.Functions.Extensions.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;
using Polly;
using Polly.Registry;
using System.Text;
using System.Text.Json;
using CosmosContainer = Microsoft.Azure.Cosmos.Container;

namespace CogniDoc.Functions.Triggers;

public class DocumentProcessorFunction
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly SearchClient _searchClient;
    private readonly CosmosContainer _cosmosContainer;
    private readonly EmbeddingClient _embeddingClient;
    private readonly IDocumentAiService _documentAiService;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly ILogger<DocumentProcessorFunction> _logger;
    private readonly AzureOpenAIOptions _options;

    public DocumentProcessorFunction(
       BlobServiceClient blobServiceClient,
        SearchClient searchClient,
        CosmosContainer cosmosContainer,
        IDocumentAiService documentAiService,
        EmbeddingClient embeddingClient,
        ResiliencePipelineProvider<string> pipelineProvider,
        IOptions<AzureOpenAIOptions> options,
        ILogger<DocumentProcessorFunction> logger)
    {
        _blobServiceClient = blobServiceClient;
        _searchClient = searchClient;
        _cosmosContainer = cosmosContainer;
        _documentAiService = documentAiService;
        _embeddingClient = embeddingClient;
        _options = options.Value;
        _logger = logger;

        _resiliencePipeline = pipelineProvider.GetPipeline(OpenAiExtensions.ResiliencePipelineName);
    }

    [Function(nameof(DocumentProcessorFunction))]
    public async Task Run(
        [ServiceBusTrigger(ServiceBusOptions.DocumentProcessingQueueTrigger, Connection = ServiceBusOptions.ConnectionKey)]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing Service Bus message ID: {MessageId}", message.MessageId);

        // 1. Deserialize and Validate Ingestion Task
        DocumentProcessingMessage? task;
        try
        {
            task = JsonSerializer.Deserialize<DocumentProcessingMessage>(message.Body.ToString(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (task == null || string.IsNullOrWhiteSpace(task.FileUrl))
            {
                throw new JsonException("Deserialized task payload was null or missing FileUrl.");
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Poison message detected ({MessageId}). Dead-lettering immediately.", message.MessageId);
            await messageActions.DeadLetterMessageAsync(
                message,
                deadLetterReason: "CorruptPayload",
                deadLetterErrorDescription: ex.Message,
                cancellationToken: cancellationToken);
            return;
        }


        string rawText;

        try
        {
            // 2. Stream File Content from Blob Storage
            rawText = await DownloadBlobTextAsync(task.FileUrl, cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogError(ex, "Source blob missing at {Url}. Dead-lettering task {JobId}.", task.FileUrl, task.JobId);
            await messageActions.DeadLetterMessageAsync(
                message,
                deadLetterReason: "BlobNotFound",
                deadLetterErrorDescription: ex.Message,
                cancellationToken: cancellationToken);
            return;
        }
        if (string.IsNullOrWhiteSpace(rawText))
        {
            _logger.LogWarning("Blob at {Url} contains no text. Completing task without indexing.", task.FileUrl);
            await messageActions.CompleteMessageAsync(message, cancellationToken);
            return;
        }

        // 3. Chunk Document Text
        List<string> textChunks = CreateChunksWithOverlap(rawText, chunkSize: 1200, overlap: 150);
        _logger.LogInformation("Document partitioned into {Count} semantic chunks.", textChunks.Count);


        // 4. Batch Vector Generation in sub-batches
        const int batchSize = 6;
        var allEmbeddings = new List<OpenAIEmbedding>();

        _logger.LogInformation("Generating embeddings for {Count} chunks in batches of {BatchSize}...", textChunks.Count, batchSize);

        for (int i = 0; i < textChunks.Count; i += batchSize)
        {
            var batch = textChunks.Skip(i).Take(batchSize).ToList();

            OpenAIEmbeddingCollection batchResult = await _resiliencePipeline.ExecuteAsync(
                async ct => await _embeddingClient.GenerateEmbeddingsAsync(batch, cancellationToken: ct),
                cancellationToken);

            allEmbeddings.AddRange(batchResult);

            // Brief delay to prevent burst RPM throttling across consecutive batches
            if (i + batchSize < textChunks.Count)
            {
                await Task.Delay(400, cancellationToken);
            }
        }

        _logger.LogInformation("Successfully generated {Count} embeddings.", allEmbeddings.Count);

        // 5. Index Document Chunks into Azure AI Search
        var searchBatch = new List<SearchDocument>();
        string fileName = Path.GetFileName(new Uri(task.FileUrl).LocalPath);

        for (int i = 0; i < textChunks.Count; i++)
        {
            searchBatch.Add(new SearchDocument
            {
                ["id"] = $"{task.JobId}_{i}",
                ["documentId"] = task.JobId,
                ["fileName"] = fileName,
                ["chunkIndex"] = i,
                ["content"] = textChunks[i],
                ["contentVector"] = allEmbeddings[i].ToFloats().ToArray()
            });
        }

        await _searchClient.UploadDocumentsAsync(searchBatch, cancellationToken: cancellationToken);
        _logger.LogInformation("Indexed {Count} vectors in Azure AI Search for Job {JobId}.", searchBatch.Count, task.JobId);

        // 6. Generate AI Summary via Domain Service
        string summary = await _documentAiService.GenerateSummaryAsync(rawText, cancellationToken);

        // 7. Persist Metadata & Audit Record in Cosmos DB
        var entity = new DocumentMetadataEntity
        {
            Id = task.JobId,
            PartitionKey = task.JobId, // Must match the value expected at /partitionKey
            DocumentId = task.JobId,
            FileName = fileName,
            Summary = summary,
            ChunkCount = textChunks.Count,
            ProcessedAt = DateTime.UtcNow
        };

        await _cosmosContainer.UpsertItemAsync(
            item: entity,
            partitionKey: new PartitionKey(entity.Id),
            cancellationToken: cancellationToken);

        _logger.LogInformation("Successfully saved metadata to Cosmos DB for Job {JobId}.", task.JobId);

        // 8. Complete Message
        await messageActions.CompleteMessageAsync(message, cancellationToken);
        _logger.LogInformation("Job {JobId} successfully processed and committed.", task.JobId);
    }

    private async Task<string> DownloadBlobTextAsync(string blobUrl, CancellationToken cancellationToken)
    {
        var blobUriBuilder = new BlobUriBuilder(new Uri(blobUrl));
        var containerClient = _blobServiceClient.GetBlobContainerClient(blobUriBuilder.BlobContainerName);
        var blobClient = containerClient.GetBlobClient(blobUriBuilder.BlobName);

        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        using var reader = new StreamReader(response.Value.Content, Encoding.UTF8);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static List<string> CreateChunksWithOverlap(string text, int chunkSize, int overlap)
    {
        var chunks = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return chunks;

        int step = Math.Max(1, chunkSize - overlap);
        for (int i = 0; i < text.Length; i += step)
        {
            int length = Math.Min(chunkSize, text.Length - i);
            chunks.Add(text.Substring(i, length));
            if (i + length >= text.Length) break;
        }

        return chunks;
    }
}