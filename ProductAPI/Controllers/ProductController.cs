using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductAPI.Models;
using ProductAPI.Repositories;
using ProductAPI.Services;
using System.Security.Claims;

namespace ProductAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ProductController : ControllerBase
{
    private readonly IProductRepository _repo;
    private readonly ElasticSearchService _elastic;

    public ProductController(IProductRepository repo, ElasticSearchService elastic)
    {
        _repo = repo;
        _elastic = elastic;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userid") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            return userId;
        throw new UnauthorizedAccessException("User ID not found in token");
    }

    // 1. GET: Standard Database Pagination (with Filtering)
    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? category = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        int? filterSellerId = null;
        // Admins only see their own products in the dashboard list
        if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
        {
            filterSellerId = GetCurrentUserId();
        }

        var pagedResult = await _repo.GetProducts(filterSellerId, category, page, pageSize);
        return Ok(pagedResult);
    }

    // 2. SEARCH: Elasticsearch (Fuzzy Search)
    [HttpGet("search")]
    public async Task<IActionResult> SearchProducts([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Query cannot be empty");

        int? filterSellerId = null;

        // CRITICAL FIX: Only Admins are restricted to their own ID.
        // Customers (who are not Admin) will keep filterSellerId = null (Search All).
        if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
        {
            filterSellerId = GetCurrentUserId();
        }

        var results = await _elastic.SearchAsync(q, filterSellerId);
        return Ok(results);
    }

    // 3. CREATE: Sync to Elastic
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> CreateProduct(Product product)
    {
        product.seller_id = GetCurrentUserId();

        var id = await _repo.CreateProduct(product);
        product.Id = id;

        // Sync to Elastic
        await _elastic.IndexProductAsync(product);

        return CreatedAtAction(nameof(GetProducts), new { id }, product);
    }

    // 4. UPDATE: Sync to Elastic
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product product)
    {
        product.Id = id;
        product.seller_id = GetCurrentUserId();

        var affected = await _repo.UpdateProduct(product);
        if (affected == 0)
            return NotFound("Product not found or permission denied.");

        // Sync to Elastic
        await _elastic.IndexProductAsync(product);

        return Ok("Product updated successfully");
    }

    // 5. DELETE: Remove from Elastic
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        int currentUserId = GetCurrentUserId();

        var affected = await _repo.DeleteProduct(id, currentUserId);
        if (affected == 0)
            return NotFound("Product not found or permission denied.");

        // Remove from Elastic
        await _elastic.DeleteProductAsync(id);

        return Ok("Product deleted");
    }

    // 6. SYNC ALL: Manual Trigger
    [HttpPost("sync-all")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> SyncAllProducts()
    {
        // Requires IProductRepository to have GetAllProducts()
        var allProducts = await _repo.GetAllProducts();

        int count = 0;
        foreach (var product in allProducts)
        {
            await _elastic.IndexProductAsync(product);
            count++;
        }

        return Ok($"Synced {count} products to Elasticsearch.");
    }
}