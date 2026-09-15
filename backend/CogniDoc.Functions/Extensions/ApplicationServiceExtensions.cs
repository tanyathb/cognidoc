using CogniDoc.Functions.Extensions.Services;
using CogniDoc.Functions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CogniDoc.Functions.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IDocumentAiService, DocumentAiService>();

            // Register other domain/business services here as your app grows
            // services.AddScoped<IDocumentValidator, DocumentValidator>();

            return services;
        }
    }
}
