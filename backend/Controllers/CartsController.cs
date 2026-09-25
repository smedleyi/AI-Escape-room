using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using StyleVerse.Backend.Data;
using StyleVerse.Backend.Models;

namespace StyleVerse.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartsController : ControllerBase
    {
        private readonly CosmosDb _db;

        public CartsController(CosmosDb db)
        {
            _db = db;
        }

        [HttpGet("{sessionId}")]
        public async Task<ActionResult<IEnumerable<CartItemView>>> GetCart(string sessionId)
        {
            if (!CosmosDb.IsValidId(sessionId)) return BadRequest();

            var cart = await CosmosDb.TryReadAsync<Cart>(_db.Carts, sessionId, sessionId);
            return (cart?.Items ?? new()).Select(l => ToView(sessionId, l)).ToList();
        }

        [HttpPost]
        public async Task<ActionResult<CartItemView>> AddToCart(AddToCartRequest request)
        {
            if (!CosmosDb.IsValidId(request.SessionId) || request.Quantity < 1) return BadRequest();

            var productId = request.ProductId.ToString();
            var product = await CosmosDb.TryReadAsync<Product>(_db.Products, productId, productId);
            if (product is null) return BadRequest("Unknown product.");

            CartLine? line = null;
            var (cart, conflict) = await UpdateCartAsync(request.SessionId, createIfMissing: true, c =>
            {
                line = c.Items.FirstOrDefault(l => l.ProductId == product.ProductId);
                if (line is null)
                {
                    line = new CartLine { ProductId = product.ProductId, Name = product.Name, Price = product.Price, Quantity = request.Quantity };
                    c.Items.Add(line);
                }
                else
                {
                    line.Quantity += request.Quantity;
                }
                return true;
            });

            if (conflict) return Conflict("Cart was modified concurrently; please retry.");
            return Ok(ToView(cart!.SessionId, line!));
        }

        [HttpDelete("{sessionId}/{productId:int}")]
        public async Task<IActionResult> RemoveFromCart(string sessionId, int productId)
        {
            if (!CosmosDb.IsValidId(sessionId)) return BadRequest();

            var (cart, conflict) = await UpdateCartAsync(sessionId, createIfMissing: false,
                c => c.Items.RemoveAll(l => l.ProductId == productId) > 0);

            if (conflict) return Conflict("Cart was modified concurrently; please retry.");
            return cart is null ? NotFound() : NoContent();
        }

        // Read-modify-write guarded by the document ETag, so two concurrent requests can't overwrite each other.
        private async Task<(Cart? Cart, bool Conflict)> UpdateCartAsync(string sessionId, bool createIfMissing, Func<Cart, bool> apply)
        {
            var partitionKey = new PartitionKey(sessionId);
            for (var attempt = 0; attempt < 5; attempt++)
            {
                if (attempt > 0) await Task.Delay(Random.Shared.Next(10, 50 * attempt));

                Cart? cart;
                string? etag = null;
                try
                {
                    var read = await _db.Carts.ReadItemAsync<Cart>(sessionId, partitionKey);
                    cart = read.Resource;
                    etag = read.ETag;
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    if (!createIfMissing) return (null, false);
                    cart = new Cart { Id = sessionId, SessionId = sessionId };
                }

                if (!apply(cart)) return (null, false);

                try
                {
                    var options = new ItemRequestOptions { IfMatchEtag = etag, EnableContentResponseOnWrite = false };
                    if (etag is null)
                        await _db.Carts.CreateItemAsync(cart, partitionKey, options);
                    else
                        await _db.Carts.ReplaceItemAsync(cart, sessionId, partitionKey, options);
                    return (cart, false);
                }
                catch (CosmosException ex) when (ex.StatusCode is HttpStatusCode.PreconditionFailed or HttpStatusCode.Conflict)
                {
                }
            }
            return (null, true);
        }

        private static CartItemView ToView(string sessionId, CartLine line) =>
            new(line.ProductId.ToString(), sessionId, line.ProductId, line.Quantity, line.DateAdded,
                new CartProductView(line.ProductId, line.Name, line.Price));
    }
}