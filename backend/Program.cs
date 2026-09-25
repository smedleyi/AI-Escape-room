using Azure.Core;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Azure.Cosmos;
using OpenTelemetry.Trace;
using StyleVerse.Backend.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Requests and Cosmos calls go to Application Insights (Application Map, Live Metrics) when it's configured.
if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);
    builder.Services.AddOpenTelemetry()
        .UseAzureMonitor()
        .WithTracing(tracing => tracing.AddSource("Azure.Cosmos.Operation"));
}

builder.Services.AddMemoryCache();

// Add services to the container.
// Setup Controllers with JSON options to prevent Reference Loops
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        // Product ids are strings in Cosmos; the React app posts them back as productId.
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Challenge 2 State: Using Azure Cosmos DB for NoSQL, authenticated with the app's Entra identity (no keys)
var cosmosEndpoint = builder.Configuration["Cosmos:Endpoint"];
if (string.IsNullOrWhiteSpace(cosmosEndpoint))
{
    throw new InvalidOperationException("Cosmos:Endpoint is not configured.");
}

// Each regional App Service sets App__Region to its own region; the SDK then reads from the nearest replica.
var appRegion = new AppRegion(builder.Configuration["App:Region"] is { Length: > 0 } r ? r : Regions.UKSouth);
builder.Services.AddSingleton(appRegion);

// On App Service use the managed identity directly (fast cold start); locally fall back to the az login.
TokenCredential credential = Environment.GetEnvironmentVariable("IDENTITY_ENDPOINT") is not null
    ? new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned)
    : new AzureCliCredential();

builder.Services.AddSingleton(new CosmosClient(cosmosEndpoint, credential, new CosmosClientOptions
{
    ApplicationName = "StyleVerse",
    ApplicationRegion = appRegion.Name,
    UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web),
    CosmosClientTelemetryOptions = new CosmosClientTelemetryOptions { DisableDistributedTracing = false },
}));
builder.Services.AddSingleton<CosmosDb>();

// CORS for Frontend
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll",
        b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().WithExposedHeaders("X-Azure-Region", "X-Cosmos-Region", "X-Continuation-Token"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

// Serve static files for the React Frontend (Deployment model)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html"); // Hooks React routing

app.Run();

public record AppRegion(string Name);