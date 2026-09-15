using CogniDoc.Functions.Extensions.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace CogniDoc.Functions.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddAzureIdentity()
            .AddCosmosPersistence(configuration)
            .AddAiSearch(configuration)
            .AddOpenAiWithResilience(configuration)
            .AddStorageAndMessaging(configuration);
    }
}