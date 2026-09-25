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

    // Newest first, one bounded page per call; pass X-Continuation-Token back as ?continuationToken= for the next page.
    // With ?email=, the query targets that single partition.
    [HttpGet]
    public async Task<ActionResult> GetOrders([FromQuery] string? email = null, [FromQuery] int limit = 100, [FromQuery] string? continuationToken = null)
    {
        if (limit is < 1 or > 500) return BadRequest("limit must be between 1 and 500.");

        var options = new QueryRequestOptions { MaxItemCount = limit };
        if (!string.IsNullOrWhiteSpace(email)) options.PartitionKey = new PartitionKey(email);

        using var feed = _db.Orders.GetItemQueryIterator<Order>("SELECT * FROM c ORDER BY c.orderDate DESC", continuationToken, options);
        var page = await feed.ReadNextAsync();
        var orders = page.ToList();
        if (page.ContinuationToken is not null) Response.Headers["X-Continuation-Token"] = page.ContinuationToken;

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
        await _db.Orders.CreateItemAsync(order, new PartitionKey(order.Email), new ItemRequestOptions { EnableContentResponseOnWrite = false });

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