using Azure.Identity;
using Azure.Messaging.ServiceBus;
using CogniDoc.Functions.Configuration;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CogniDoc.Functions.Extensions.Infrastructure;

public static class MessagingExtensions
{
    public static IServiceCollection AddStorageAndMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Bind Options
        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName));

        services.AddOptions<ServiceBusOptions>()
            .Bind(configuration.GetSection(ServiceBusOptions.SectionName));

        // 2. Azure SDK Client Factory
        services.AddAzureClients(clientBuilder =>
        {
            // Entra ID fallback credentials for deployed environments
            clientBuilder.UseCredential(new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeManagedIdentityCredential = true,
                ExcludeWorkloadIdentityCredential = true,
                ExcludeEnvironmentCredential = true
            }));

            // --- BlobServiceClient ---
            string? blobConnection = configuration[StorageOptions.ConnectionKey];

            if (!string.IsNullOrWhiteSpace(blobConnection) &&
                !blobConnection.Equals("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase))
            {
                // Local Dev: Shared Key
                clientBuilder.AddBlobServiceClient(blobConnection);
            }
            else
            {
                // Fallback / Cloud: Blob Endpoint URI
                string blobEndpoint = configuration[StorageOptions.EndpointKey]
                    ?? throw new InvalidOperationException($"Neither '{StorageOptions.ConnectionKey}' nor '{StorageOptions.EndpointKey}' was configured.");

                clientBuilder.AddBlobServiceClient(new Uri(blobEndpoint));
            }

            // --- ServiceBusClient ---
            string? sbConnectionString = configuration[ServiceBusOptions.ConnectionKey];
            string? sbNamespace = configuration[ServiceBusOptions.NamespaceKey];

            if (!string.IsNullOrWhiteSpace(sbConnectionString))
            {
                // Local Dev: Shared Access Key
                clientBuilder.AddServiceBusClient(sbConnectionString);
            }
            else if (!string.IsNullOrWhiteSpace(sbNamespace))
            {
                // Fallback / Cloud: Fully Qualified Namespace
                clientBuilder.AddServiceBusClientWithNamespace(sbNamespace);
            }
            else
            {
                throw new InvalidOperationException($"Neither '{ServiceBusOptions.ConnectionKey}' nor '{ServiceBusOptions.NamespaceKey}' was configured.");
            }
        });

        // 3. Register ServiceBusSender for BlobIngestionFunction
        services.AddSingleton(sp =>
        {
            var client = sp.GetRequiredService<ServiceBusClient>();
            var queueOptions = sp.GetRequiredService<IOptions<ServiceBusOptions>>().Value;

            return client.CreateSender(queueOptions.DocumentProcessingQueueName);
        });

        return services;
    }
}