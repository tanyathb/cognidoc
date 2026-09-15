using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CogniDoc.Functions.Extensions.Infrastructure;

public static class IdentityExtensions
{
    public static IServiceCollection AddAzureIdentity(this IServiceCollection services)
    {
        services.AddSingleton<DefaultAzureCredential>(_ =>
        {
            var options = new DefaultAzureCredentialOptions
            {
                // Disable IMDS probing on developer machines to stop the 21s network hang
                ExcludeManagedIdentityCredential = true,
                ExcludeWorkloadIdentityCredential = true,
                ExcludeInteractiveBrowserCredential = true,
                Diagnostics = { IsLoggingEnabled = true }
            };
            return new DefaultAzureCredential(options);
        });

        return services;
    }
}