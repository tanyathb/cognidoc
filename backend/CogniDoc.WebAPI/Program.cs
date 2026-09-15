using Azure.Identity;
using CogniDoc.WebAPI.Configuration;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

// --- 1. ENVIRONMENT VERIFICATION LOGGING ---
Console.WriteLine("==================================================");
Console.WriteLine($"Active Environment : {builder.Environment.EnvironmentName}");
Console.WriteLine($"IsDevelopment      : {builder.Environment.IsDevelopment()}");
Console.WriteLine($"IsProduction       : {builder.Environment.IsProduction()}");
Console.WriteLine($"Blob Connection    : {builder.Configuration.GetConnectionString("AzureStorage") ?? "(null - appsettings.Development.json not active)"}");
Console.WriteLine($"Blob Endpoint      : {builder.Configuration["Storage:BlobEndpoint"] ?? "(null)"}");
Console.WriteLine("==================================================");

// --- 2. CORS CONFIGURATION ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            var uri = new Uri(origin);
            return uri.Host == "localhost" || uri.Host.EndsWith("azurestaticapps.net");
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

builder.Services.Configure<StorageOptions>(
    builder.Configuration.GetSection(StorageOptions.SectionName));

// --- 3. EXPLICIT ENVIRONMENT-BASED CLIENT REGISTRATION ---
builder.Services.AddAzureClients(clientBuilder =>
{
    if (builder.Environment.IsDevelopment())
    {
        // Local machine: Read connection string from appsettings.Development.json
        var localConnection = builder.Configuration.GetConnectionString("AzureStorage")
                              ?? "UseDevelopmentStorage=true";

        Console.WriteLine($"[Storage] Registering local Azurite/Key connection: {localConnection}");
        clientBuilder.AddBlobServiceClient(localConnection);
    }
    else
    {
        // Azure App Service: Passwordless via Managed Identity & BlobEndpoint
        var endpoint = builder.Configuration["Storage:BlobEndpoint"]
            ?? throw new InvalidOperationException("Storage:BlobEndpoint configuration is missing in Azure App Settings.");

        Console.WriteLine($"[Storage] Registering Cloud BlobServiceClient with Managed Identity: {endpoint}");
        clientBuilder.AddBlobServiceClient(new Uri(endpoint));
        clientBuilder.UseCredential(new DefaultAzureCredential());
    }
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowReactApp");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();