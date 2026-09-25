using Microsoft.AspNetCore.Mvc;
using StyleVerse.Backend.Data;
using StyleVerse.Backend.Models;

namespace StyleVerse.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppRegion _region;
    private readonly CosmosDb _db;

    public HealthController(AppRegion region, CosmosDb db)
    {
        _region = region;
        _db = db;
    }

    // Front Door probes this from every edge location, so it deliberately does not touch Cosmos.
    [HttpGet]
    public IActionResult Get()
    {
        Response.Headers["X-Azure-Region"] = _region.Name;
        return Ok(new { status = "ok", region = _region.Name });
    }

    // Diagnostic: one ~1 RU point read, reporting which Cosmos replica served it.
    [HttpGet("cosmos")]
    public async Task<IActionResult> GetCosmos()
    {
        var response = await _db.Products.ReadItemAsync<Product>("1", new Microsoft.Azure.Cosmos.PartitionKey("1"));
        var contacted = response.Diagnostics.GetContactedRegions().Select(r => r.regionName).Distinct().ToList();

        Response.Headers["X-Azure-Region"] = _region.Name;
        Response.Headers["X-Cosmos-Region"] = string.Join(",", contacted);
        return Ok(new
        {
            appRegion = _region.Name,
            cosmosRegions = contacted,
            elapsedMs = Math.Round(response.Diagnostics.GetClientElapsedTime().TotalMilliseconds, 1),
            requestCharge = response.RequestCharge,
        });
    }
}
