using Azure.Messaging.ServiceBus;
using CogniDoc.Application.Models;
using CogniDoc.Functions.Configuration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CogniDoc.Functions.Triggers;

public class BlobIngestionFunction
{
    private readonly ILogger<BlobIngestionFunction> _logger;
    private readonly ServiceBusSender _serviceBusSender;
    private readonly ServiceBusOptions _sbOptions;

    public BlobIngestionFunction(
        ILogger<BlobIngestionFunction> logger,
        ServiceBusSender serviceBusSender,
        IOptions<ServiceBusOptions> sbOptions)
    {
        _logger = logger;
        _serviceBusSender = serviceBusSender;
        _sbOptions = sbOptions.Value;
    }

    [Function(nameof(BlobIngestionFunction))]
    public async Task Run([BlobTrigger(StorageOptions.ContainerTrigger, Connection = StorageOptions.ConnectionKey)] Stream stream, string name, Uri uri)
    {
        _logger.LogInformation("Blob detected: {FileName} ({Bytes} bytes). URI: {Uri}", name, stream.Length, uri);
        _logger.LogInformation("Enqueuing to target queue: {Queue}", _sbOptions.DocumentProcessingQueueName);

        try
        {
            var processingTaskask = new DocumentProcessingMessage
            {
                JobId = Guid.NewGuid().ToString("N"),
                FileUrl = uri.ToString(),
                FileSizeBytes = stream.Length,
                ContentType = DetermineContentType(name),
                TriggeredAt = DateTime.UtcNow
            };

            string messageBody = JsonSerializer.Serialize(processingTaskask);
            var serviceBusMessage = new ServiceBusMessage(messageBody)
            {
                MessageId = processingTaskask.JobId, // Guarantees message deduplication in Service Bus
                ContentType = "application/json",
                Subject = "DocumentUploaded"
            };

            await _serviceBusSender.SendMessageAsync(serviceBusMessage);

            _logger.LogInformation("Successfully enqueued Job {JobId} for file {FileName}",
                processingTaskask.JobId, name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue processing task for blob {FileName}", name);
            throw; // Re-throw to let the Functions runtime manage retries or poison queueing
        }
    }

    private static string DetermineContentType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            ".json" => "application/json",
            _ => "application/octet-stream"
        };
    }
}