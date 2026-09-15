using Azure.AI.OpenAI;
using CogniDoc.Functions.Configuration;
using CogniDoc.Functions.Extensions.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using Polly;
using Polly.Registry;
using System.ClientModel;

namespace CogniDoc.Functions.Services;

public class DocumentAiService : IDocumentAiService
{
    private const string PipelineKey = "azure-openai-pipeline";
    private readonly ChatClient _chatClient;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly ILogger<DocumentAiService> _logger;

    public DocumentAiService(
        AzureOpenAIClient openAiClient,
        ResiliencePipelineProvider<string> pipelineProvider,
        IOptions<AzureOpenAIOptions> openAiOptions,
        ILogger<DocumentAiService> logger)
    {
        _logger = logger;

        // Retrieve the pre-configured Polly v8 pipeline
        _resiliencePipeline = pipelineProvider.GetPipeline(PipelineKey);

        // Resolve chat deployment name (defaulting to gpt-5-mini if unspecified)
        string deploymentName = !string.IsNullOrWhiteSpace(openAiOptions.Value.ChatDeploymentName)
            ? openAiOptions.Value.ChatDeploymentName
            : "gpt-5-mini";

        _chatClient = openAiClient.GetChatClient(deploymentName);
    }

    public async Task<string> GenerateSummaryAsync(string documentText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentText))
        {
            return string.Empty;
        }

        // Guard against massive payloads blowing the LLM context window
        string sanitizedText = documentText.Length > 12000
         ? documentText[..12000] + "\n...[Content truncated for summary]..."
         : documentText;

        _logger.LogInformation("Generating AI summary for document text ({Length} characters).", sanitizedText.Length);

        // Execute LLM call inside the Polly v8 resilience pipeline
        return await _resiliencePipeline.ExecuteAsync(async token =>
        {
            var messages = new List<ChatMessage>
            {
                //ChatMessage.CreateSystemMessage(
                //    "You are an enterprise AI assistant. Analyze the provided document text and produce " +
                //    "a concise executive summary in 3-5 bullet points focusing on key outcomes, entities, and actions."),
                //ChatMessage.CreateUserMessage(
                //    $"Generate an executive summary for this document:\n\n{sanitizedText}")

                new SystemChatMessage("You are an executive document summarizer. Rely strictly and solely on the provided text. " +
                "Do not extrapolate, assume, or hallucinate details not explicitly stated in the source. " +
                "Provide a concise executive summary formatted as 3 to 5 clear bullet points.")
            };

            var chatOptions = new ChatCompletionOptions
            {
                //Temperature = 0.2f, // Low temperature for deterministic, factual extraction
                //MaxOutputTokenCount = 400
            };

            ClientResult<ChatCompletion> result = await _chatClient.CompleteChatAsync(
                messages,
                chatOptions,
                cancellationToken: token);

            return result.Value.Content[0].Text.Trim();
        }, cancellationToken);
    }
}