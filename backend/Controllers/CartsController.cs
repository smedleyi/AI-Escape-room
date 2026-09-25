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

            var cart = await CosmosDb.TryReadAsync<Cart>(_db.Carts, request.SessionId, request.SessionId)
                ?? new Cart { Id = request.SessionId, SessionId = request.SessionId };

            var line = cart.Items.FirstOrDefault(l => l.ProductId == product.ProductId);
            if (line is null)
            {
                line = new CartLine { ProductId = product.ProductId, Name = product.Name, Price = product.Price, Quantity = request.Quantity };
                cart.Items.Add(line);
            }
            else
            {
                line.Quantity += request.Quantity;
            }

            await _db.Carts.UpsertItemAsync(cart, new PartitionKey(cart.SessionId));
            return Ok(ToView(cart.SessionId, line));
        }

        [HttpDelete("{sessionId}/{productId:int}")]
        public async Task<IActionResult> RemoveFromCart(string sessionId, int productId)
        {
            if (!CosmosDb.IsValidId(sessionId)) return BadRequest();

            var cart = await CosmosDb.TryReadAsync<Cart>(_db.Carts, sessionId, sessionId);
            if (cart is null || cart.Items.RemoveAll(l => l.ProductId == productId) == 0) return NotFound();

            await _db.Carts.UpsertItemAsync(cart, new PartitionKey(cart.SessionId));
            return NoContent();
        }

        private static CartItemView ToView(string sessionId, CartLine line) =>
            new(line.ProductId.ToString(), sessionId, line.ProductId, line.Quantity, line.DateAdded,
                new CartProductView(line.ProductId, line.Name, line.Price));
    }
}