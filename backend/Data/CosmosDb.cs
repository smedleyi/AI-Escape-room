using System.Net;
using Microsoft.Azure.Cosmos;

namespace StyleVerse.Backend.Data;

public class CosmosDb
{
    public CosmosDb(CosmosClient client, IConfiguration config)
    {
        var database = client.GetDatabase(config["Cosmos:Database"] ?? "StyleVerseDb");
        Products = database.GetContainer("Products");
        Carts = database.GetContainer("Carts");
        Orders = database.GetContainer("Orders");
    }

    public Container Products { get; }
    public Container Carts { get; }
    public Container Orders { get; }

    public static async Task<T?> TryReadAsync<T>(Container container, string id, string partitionKey) where T : class
    {
        try
        {
            return (await container.ReadItemAsync<T>(id, new PartitionKey(partitionKey))).Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public static async Task<List<T>> ReadAllAsync<T>(Container container, string? partitionKey = null)
    {
        var options = partitionKey is null ? null : new QueryRequestOptions { PartitionKey = new PartitionKey(partitionKey) };
        var results = new List<T>();
        using var feed = container.GetItemQueryIterator<T>("SELECT * FROM c", requestOptions: options);
        while (feed.HasMoreResults)
        {
            results.AddRange(await feed.ReadNextAsync());
        }
        return results;
    }

    // Cosmos ids cannot contain these characters.
    public static bool IsValidId(string? id) =>
        !string.IsNullOrWhiteSpace(id) && id.Length <= 255 && id.IndexOfAny(['/', '\\', '?', '#']) < 0;
}
