using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Caching.Memory;
using StyleVerse.Backend.Data;
using StyleVerse.Backend.Models;

namespace StyleVerse.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private const string ProductListCacheKey = "products:all";
        private static readonly TimeSpan ProductListTtl = TimeSpan.FromSeconds(10);

        private readonly CosmosDb _db;
        private readonly IMemoryCache _cache;

        public ProductsController(CosmosDb db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        // Listing all products spans partitions by nature (pk is /id), so it's cached briefly per instance.
        // The Task itself is cached so concurrent misses share one Cosmos query instead of stampeding.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductView>>> GetProducts()
        {
            var load = _cache.GetOrCreate(ProductListCacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = ProductListTtl;
                return LoadProductListAsync();
            })!;

            try
            {
                return await load;
            }
            catch
            {
                _cache.Remove(ProductListCacheKey);
                throw;
            }
        }

        private async Task<List<ProductView>> LoadProductListAsync()
        {
            var products = await CosmosDb.ReadAllAsync<Product>(_db.Products);
            return products.OrderBy(p => p.ProductId).Select(ProductView.From).ToList();
        }

        // Point read: id is also the partition key, so this costs ~1 RU.
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductView>> GetProduct(string id)
        {
            if (!CosmosDb.IsValidId(id)) return BadRequest();

            var product = await CosmosDb.TryReadAsync<Product>(_db.Products, id, id);
            return product is null ? NotFound() : ProductView.From(product);
        }

        [HttpPost]
        public Task<ActionResult<ProductView>> UpsertProduct(ProductUpsertRequest request) => Upsert(null, request);

        [HttpPut("{id}")]
        public Task<ActionResult<ProductView>> UpsertProductById(string id, ProductUpsertRequest request) => Upsert(id, request);

        private async Task<ActionResult<ProductView>> Upsert(string? routeId, ProductUpsertRequest request)
        {
            var bodyId = request.IdAsString();
            if (routeId is not null && bodyId is not null && routeId != bodyId)
                return BadRequest("Route id and body id do not match.");

            var id = routeId ?? bodyId ?? request.ProductId?.ToString() ?? Guid.NewGuid().ToString();
            if (!CosmosDb.IsValidId(id)) return BadRequest("Invalid id.");
            if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest("Name is required.");
            if (request.Price < 0 || request.InventoryCount < 0) return BadRequest("Price and inventoryCount cannot be negative.");

            var createdDate = request.CreatedDate
                ?? (await CosmosDb.TryReadAsync<Product>(_db.Products, id, id))?.CreatedDate
                ?? DateTime.UtcNow;

            var product = new Product
            {
                Id = id,
                ProductId = request.ProductId ?? (int.TryParse(id, out var numericId) ? numericId : 0),
                CategoryId = request.CategoryId ?? 0,
                Category = request.Category ?? string.Empty,
                Name = request.Name,
                Description = request.Description ?? string.Empty,
                Price = request.Price ?? 0,
                InventoryCount = request.InventoryCount ?? 0,
                CreatedDate = createdDate,
                Tags = request.Tags ?? new(),
            };

            var response = await _db.Products.UpsertItemAsync(product, new PartitionKey(product.Id));
            _cache.Remove(ProductListCacheKey);
            var view = ProductView.From(response.Resource);

            return response.StatusCode == HttpStatusCode.Created
                ? CreatedAtAction(nameof(GetProduct), new { id = view.Id }, view)
                : Ok(view);
        }
    }
}