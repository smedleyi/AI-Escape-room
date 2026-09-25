namespace StyleVerse.Backend.Models;

// Cosmos document in the Orders container (partition key /email); line items are embedded.
public class Order
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Type { get; set; } = "order";
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string ShippingRegion { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public List<OrderItem> OrderItems { get; set; } = new();
}

public class OrderItem
{
    public int ProductId { get; set; }
    public string? Name { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
