using Azure;
using Azure.Identity;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Logging;

namespace CogniDoc.Infrastructure.Services;

public class AzureSearchIndexService
{
    private readonly SearchIndexClient _indexClient;
    private readonly ILogger<AzureSearchIndexService> _logger;

    public AzureSearchIndexService(SearchIndexClient indexClient, ILogger<AzureSearchIndexService> logger)
    {
        _indexClient = indexClient;
        _logger = logger;
    }

    public async Task CreateOrUpdateIndexAsync(string indexName, CancellationToken cancellationToken = default)
    {
        const string vectorConfigName = "cognidoc-hnsw-config";
        const string vectorProfileName = "cognidoc-vector-profile";

        _logger.LogInformation("Ensuring Azure AI Search index '{IndexName}' exists...", indexName);

        // 1. Define Fields
        var fields = new List<SearchField>
        {
            new SimpleField("id", SearchFieldDataType.String)
            {
                IsKey = true,
                IsFilterable = true
            },
            new SimpleField("documentId", SearchFieldDataType.String)
            {
                IsFilterable = true
            },
            new SearchableField("fileName")
            {
                IsFilterable = true,
                IsSortable = true,
                IsFacetable = true
            },
            new SearchableField("content")
            {
                IsFilterable = false
            },
            // Vector Field: 3072 dimensions for Azure OpenAI text-embedding-3-large
            new VectorSearchField("contentVector", 3072, vectorProfileName),
            new SimpleField("chunkIndex", SearchFieldDataType.Int32)
            {
                IsFilterable = true,
                IsSortable = true
            },
            new SimpleField("lastUpdated", SearchFieldDataType.DateTimeOffset)
            {
                IsFilterable = true,
                IsSortable = true
            }
        };

        // 2. Configure Vector Search (HNSW Algorithm + Profile)
        var vectorSearch = new VectorSearch
        {
            Profiles =
            {
                new VectorSearchProfile(vectorProfileName, vectorConfigName)
            },
            Algorithms =
            {
                new HnswAlgorithmConfiguration(vectorConfigName)
                {
                    Parameters = new HnswParameters
                    {
                        M = 4,                       // Bi-directional links per vector node
                        EfConstruction = 400,        // Build-time accuracy vs speed trade-off
                        EfSearch = 500,              // Search-time recall quality
                        Metric = VectorSearchAlgorithmMetric.Cosine
                    }
                }
            }
        };

        // 3. Assemble Index Schema
        var indexSchema = new SearchIndex(indexName, fields)
        {
            VectorSearch = vectorSearch
        };

        // 4. Create or Update in Azure AI Search
        Response<SearchIndex> response = await _indexClient.CreateOrUpdateIndexAsync(indexSchema, cancellationToken: cancellationToken);
        _logger.LogInformation("Successfully provisioned AI Search index '{IndexName}'.", response.Value.Name);
    }
}