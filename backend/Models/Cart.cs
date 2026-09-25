namespace StyleVerse.Backend.Models;

// Cosmos document in the Carts container: one per session (id and partition key = sessionId).
public class Cart
{
    public string Id { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string Type { get; set; } = "cart";
    public List<CartLine> Items { get; set; } = new();
}

public class CartLine
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
}

public class AddToCartRequest
{
    public string SessionId { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
}

// Shape the React cart expects: item.id, item.productId, item.quantity, item.product.{name,price}.
public record CartItemView(string Id, string SessionId, int ProductId, int Quantity, DateTime DateAdded, CartProductView Product);

public record CartProductView(int Id, string Name, decimal Price);
