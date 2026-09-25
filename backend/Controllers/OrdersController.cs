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

    // With ?email=, the query targets that single partition; without it, all orders are listed.
    [HttpGet]
    public async Task<ActionResult> GetOrders([FromQuery] string? email = null)
    {
        var orders = (await CosmosDb.ReadAllAsync<Order>(_db.Orders, string.IsNullOrWhiteSpace(email) ? null : email))
            .OrderBy(o => o.OrderDate).ToList();

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

        var productIds = order.OrderItems.Select(i => i.ProductId.ToString()).Distinct().ToList();
        if (productIds.Count > 0)
        {
            var products = await _db.Products.ReadManyItemsAsync<Product>(
                productIds.Select(id => (id, new PartitionKey(id))).ToList());
            var names = products.ToDictionary(p => p.ProductId, p => p.Name);
            foreach (var item in order.OrderItems)
            {
                item.Name = names.GetValueOrDefault(item.ProductId);
            }
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