using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using CogniDoc.Functions.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using System.ClientModel;
using System.ClientModel.Primitives;

namespace CogniDoc.Functions.Extensions.Infrastructure;

public static class OpenAiExtensions
{
    public const string ResiliencePipelineName = "azure-openai-pipeline";

    public static IServiceCollection AddOpenAiWithResilience(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Strongly Typed Options
        services.AddOptions<AzureOpenAIOptions>()
            .Bind(configuration.GetSection(AzureOpenAIOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 2. Resilience Pipeline
        services.AddResiliencePipeline(ResiliencePipelineName, (builder, context) =>
        {
            // Total pipeline timeout (covers all retry attempts + backoff delays)
            builder.AddTimeout(TimeSpan.FromSeconds(180));

            // Retry strategy for rate limits (429), transient server errors (5xx), and per-attempt timeouts
            builder.AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
        .Handle<ClientResultException>(ex => ex.Status == 429 || ex.Status >= 500)
        .Handle<RequestFailedException>(ex => ex.Status == 429 || ex.Status >= 500)
        .Handle<HttpRequestException>()
        .Handle<TimeoutRejectedException>(),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Constant,
                DelayGenerator = args =>
                {
                    // When throttled, wait 60s for Azure's token bucket to replenish
                    if (args.Outcome.Exception is ClientResultException cre && cre.Status == 429)
                    {
                        return new ValueTask<TimeSpan?>(TimeSpan.FromSeconds(60));
                    }

                    if (args.Outcome.Exception is RequestFailedException rfe && rfe.Status == 429)
                    {
                        return new ValueTask<TimeSpan?>(TimeSpan.FromSeconds(60));
                    }

                    // Standard 3s delay for transient network/5xx glitches
                    return new ValueTask<TimeSpan?>(TimeSpan.FromSeconds(3));
                },
                OnRetry = args =>
                {
                    var logger = context.ServiceProvider
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("AzureOpenAIResilience");

                    logger.LogWarning(
                        "OpenAI call throttled. Waiting {Delay}s before retry attempt {Attempt}...",
                        args.RetryDelay.TotalSeconds,
                        args.AttemptNumber);

                    return ValueTask.CompletedTask;
                }
            });

            // Per-attempt timeout: if an attempt hangs at the gateway, cancel it so Retry can fire
            builder.AddTimeout(TimeSpan.FromSeconds(200));
        });

        // 3. AzureOpenAIClient Singleton
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureOpenAIOptions>>().Value;
            var config = sp.GetRequiredService<IConfiguration>();

            string endpoint = !string.IsNullOrWhiteSpace(options.Endpoint)
                ? options.Endpoint
                : config["AzureOpenAI:Endpoint"]
                    ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is missing.");

            string? apiKey = !string.IsNullOrWhiteSpace(options.ApiKey)
                ? options.ApiKey
                : config["AzureOpenAI:ApiKey"];

            var clientOptions = new AzureOpenAIClientOptions
            {
                // Disable SDK internal retries so Polly has exclusive control over retries and backoff
                RetryPolicy = new ClientRetryPolicy(maxRetries: 0)
            };

            // Local Dev: Authenticate via API key with clientOptions applied
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                return new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey), clientOptions);
            }

            // Cloud: Authenticate via Managed Identity
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeInteractiveBrowserCredential = true
            });

            return new AzureOpenAIClient(new Uri(endpoint), credential, clientOptions);
        });

        // 4. EmbeddingClient Singleton
        services.AddSingleton(sp =>
        {
            var client = sp.GetRequiredService<AzureOpenAIClient>();
            var options = sp.GetRequiredService<IOptions<AzureOpenAIOptions>>().Value;
            var config = sp.GetRequiredService<IConfiguration>();

            string deployment = !string.IsNullOrWhiteSpace(options.EmbeddingDeploymentName)
                ? options.EmbeddingDeploymentName
                : config["AzureOpenAI:EmbeddingDeploymentName"] ?? "text-embedding-3-large";

            return client.GetEmbeddingClient(deployment);
        });

        return services;
    }
}