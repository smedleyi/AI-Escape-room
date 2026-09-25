using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using StyleVerse.Backend.Data;
using StyleVerse.Backend.Models;
using StyleVerse.Backend.Common.Security.System.Win;

namespace StyleVerse.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly CosmosDb _db;

    public OrdersController(CosmosDb db) => _db = db;

    [HttpGet]
    public async Task<ActionResult> GetOrders()
    {
        var orders = (await CosmosDb.ReadAllAsync<Order>(_db.Orders)).OrderBy(o => o.OrderDate).ToList();

        // Perform Security Check
        if (!SecurityCheck.OrderSecurityCheck(orders))
        {
            return Forbid("Security check failed for orders.");
        }

        return Ok(orders);
    }

    [HttpPost]
    public async Task<ActionResult> PlaceOrder(Order order)
    {
        // In the legacy system, we calculate the total synchronously 
        // This is a bottleneck we will address in later phases.
        if (string.IsNullOrWhiteSpace(order.Email)) return BadRequest("Email is required.");

        order.Id = Guid.NewGuid().ToString();
        order.Type = "order";
        order.OrderDate = DateTime.UtcNow;

        foreach (var item in order.OrderItems)
        {
            var productId = item.ProductId.ToString();
            item.Name = (await CosmosDb.TryReadAsync<Product>(_db.Products, productId, productId))?.Name;
        }

        // 1. Save the Order first, so a failed cart delete can never lose an order
        await _db.Orders.CreateItemAsync(order, new PartitionKey(order.Email));

        // 2. Clear the cart. In our current React app, CustomerName carries the SessionId
        if (CosmosDb.IsValidId(order.CustomerName))
        {
            try
            {
                await _db.Carts.DeleteItemAsync<Cart>(order.CustomerName, new PartitionKey(order.CustomerName));
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
            }
        }

        return CreatedAtAction(nameof(GetOrders), new { id = order.Id }, order);
    }
}