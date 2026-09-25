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
}