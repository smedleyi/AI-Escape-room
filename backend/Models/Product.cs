using System.Text.Json;
using System.Text.Json.Serialization;

namespace StyleVerse.Backend.Models;

// Cosmos document in the Products container (partition key /id); category and tags are embedded.
public class Product
{
    public string Id { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string Type { get; set; } = "product";
    public int CategoryId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int InventoryCount { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public List<string> Tags { get; set; } = new();

    // System property set by Cosmos; read-only, never written back.
    [JsonPropertyName("_rid")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Rid { get; set; }
}

// API shape for products: the document fields plus cosmosdb_id (the document's _rid).
public record ProductView(
    string Id,
    int ProductId,
    string Type,
    int CategoryId,
    string Category,
    string Name,
    string Description,
    decimal Price,
    int InventoryCount,
    DateTime CreatedDate,
    List<string> Tags,
    [property: JsonPropertyName("cosmosdb_id")] string? CosmosDbId)
{
    public static ProductView From(Product p) =>
        new(p.Id, p.ProductId, p.Type, p.CategoryId, p.Category, p.Name, p.Description,
            p.Price, p.InventoryCount, p.CreatedDate, p.Tags, p.Rid);
}

public class ProductUpsertRequest
{
    // Accepts "28" or 28.
    public JsonElement? Id { get; set; }
    public int? ProductId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public int? CategoryId { get; set; }
    public string? Category { get; set; }
    public int? InventoryCount { get; set; }
    public DateTime? CreatedDate { get; set; }
    public List<string>? Tags { get; set; }

    public string? IdAsString() => Id?.ValueKind switch
    {
        JsonValueKind.String => Id.Value.GetString(),
        JsonValueKind.Number => Id.Value.GetRawText(),
        _ => null,
    };
}