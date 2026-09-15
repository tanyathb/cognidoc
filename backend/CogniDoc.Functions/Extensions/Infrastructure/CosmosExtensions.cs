using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class CosmosExtensions
{
    public static IServiceCollection AddCosmosPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(sp =>
        {
            string endpoint = configuration["CosmosDb:Endpoint"]
                ?? throw new InvalidOperationException("CosmosDb:Endpoint is missing.");
            string? authKey = configuration["CosmosDb:AuthKey"];

            var clientOptions = new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            };

            // Local Dev: Fast auth via primary key
            if (!string.IsNullOrWhiteSpace(authKey))
            {
                return new CosmosClient(endpoint, authKey, clientOptions);
            }

            // Cloud: Managed Identity
            var credential = sp.GetRequiredService<DefaultAzureCredential>();
            return new CosmosClient(endpoint, credential, clientOptions);
        });

        services.AddSingleton(sp =>
        {
            var client = sp.GetRequiredService<CosmosClient>();
            string dbName = configuration["CosmosDb:DatabaseName"] ?? "CogniDocDb";
            string containerName = configuration["CosmosDb:ContainerName"] ?? "Documents";

            return client.GetContainer(dbName, containerName);
        });

        return services;
    }
}