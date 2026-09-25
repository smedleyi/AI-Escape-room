using Microsoft.AspNetCore.Mvc;
using StyleVerse.Backend.Data;
using StyleVerse.Backend.Models;

namespace StyleVerse.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly CosmosDb _db;

        public ProductsController(CosmosDb db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            var products = await CosmosDb.ReadAllAsync<Product>(_db.Products);
            return products.OrderBy(p => p.ProductId).ToList();
        }

        // Point read: id is also the partition key, so this costs ~1 RU.
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(string id)
        {
            if (!CosmosDb.IsValidId(id)) return BadRequest();

            var product = await CosmosDb.TryReadAsync<Product>(_db.Products, id, id);
            return product is null ? NotFound() : product;
        }
    }
}