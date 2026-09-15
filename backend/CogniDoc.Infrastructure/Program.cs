using Azure.Identity;
using Azure.Search.Documents.Indexes;
using CogniDoc.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureServices((context, services) =>
    {
        // 1. Register Azure AI Search Index Client with Keyless Auth
        services.AddSingleton(sp =>
        {
            string searchEndpoint = context.Configuration["SearchService:serviceUri"]
                ?? throw new InvalidOperationException("SearchService:serviceUri is not configured.");

            return new SearchIndexClient(new Uri(searchEndpoint), new DefaultAzureCredential());
        });

        // 2. Register Indexing Management Service
        services.AddSingleton<AzureSearchIndexService>();
    })
    .Build();

host.Run();