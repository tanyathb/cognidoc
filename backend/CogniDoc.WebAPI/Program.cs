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
            if (string.IsNullOrWhiteSpace(origin)) return false;

            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            {
                return false;
            }

            var host = uri.Host;
            return host == "localhost"
                || host == "azurestaticapps.net"
                || host.EndsWith(".azurestaticapps.net");
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
        var localConnection = builder.Configuration.GetConnectionString("AzureStorage")
                              ?? "UseDevelopmentStorage=true";

        Console.WriteLine($"[Storage] Registering local Azurite/Key connection: {localConnection}");
        clientBuilder.AddBlobServiceClient(localConnection);
    }
    else
    {
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

// --- 4. MIDDLEWARE PIPELINE (CORRECT ORDER) ---
app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("AllowReactApp");

app.UseAuthorization();

app.MapControllers();

app.Run();