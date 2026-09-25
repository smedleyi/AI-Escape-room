using Azure.Identity;
using Microsoft.Azure.Cosmos;
using StyleVerse.Backend.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddSingleton(new CosmosClient(cosmosEndpoint, new DefaultAzureCredential(), new CosmosClientOptions
{
    ApplicationName = "StyleVerse",
    ApplicationRegion = appRegion.Name,
    UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web),
}));
builder.Services.AddSingleton<CosmosDb>();

// CORS for Frontend
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll",
        b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().WithExposedHeaders("X-Azure-Region", "X-Cosmos-Region"));
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