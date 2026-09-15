using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CogniDoc.Functions.Extensions.Infrastructure;

public static class SearchExtensions
{
    public static IServiceCollection AddAiSearch(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(sp =>
        {
            string endpoint = configuration["SearchServiceEndpoint"]
                ?? throw new InvalidOperationException("SearchServiceEndpoint is missing.");
            string indexName = configuration["SearchIndexName"] ?? "documents-index";
            string? apiKey = configuration["SearchServiceApiKey"];

            // Local Dev: Instant auth via admin key (bypasses Entra ID/WAM completely)
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                return new SearchClient(new Uri(endpoint), indexName, new AzureKeyCredential(apiKey));
            }

            // Cloud: Managed Identity
            var credential = sp.GetRequiredService<DefaultAzureCredential>();
            return new SearchClient(new Uri(endpoint), indexName, credential);
        });

        return services;
    }
}