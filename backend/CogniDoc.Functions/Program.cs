using Azure.Identity;
using CogniDoc.Functions.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        var credential = new DefaultAzureCredential();
        var configuration = context.Configuration;

        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddInfrastructureServices(context.Configuration);
        services.AddApplicationServices();
    }).Build();


await host.RunAsync();
