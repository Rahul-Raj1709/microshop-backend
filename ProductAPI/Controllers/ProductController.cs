using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductAPI.Models;
using ProductAPI.Repositories;
using ProductAPI.Services;
using System.Security.Claims;

namespace ProductAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // 🔒 Default: Blocks everything unless specified otherwise
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
    [AllowAnonymous] // ✅ ADD THIS: Allows public access
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? category = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        int? filterSellerId = null;

        // This check is safe: unauthenticated users return 'false' for IsInRole,
        // so we skip the block and simply return all products (filterSellerId = null).
        if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
        {
            filterSellerId = GetCurrentUserId();
        }

        var pagedResult = await _repo.GetProducts(filterSellerId, category, page, pageSize);
        return Ok(pagedResult);
    }

    // 2. SEARCH: Elasticsearch (Fuzzy Search)
    [HttpGet("search")]
    [AllowAnonymous] // ✅ ADD THIS: Allows public access
    public async Task<IActionResult> SearchProducts([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Query cannot be empty");

        int? filterSellerId = null;

        if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
        {
            filterSellerId = GetCurrentUserId();
        }

        var results = await _elastic.SearchAsync(q, filterSellerId);
        return Ok(results);
    }

    // 3. CREATE: Sync to Elastic
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")] // Remains protected
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
    [Authorize(Roles = "SuperAdmin,Admin")] // Remains protected
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
    [Authorize(Roles = "SuperAdmin,Admin")] // Remains protected
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
    [Authorize(Roles = "SuperAdmin,Admin")] // Remains protected
    public async Task<IActionResult> SyncAllProducts()
    {
        var allProducts = await _repo.GetAllProducts();
        await _elastic.BulkIndexProductsAsync(allProducts);
        return Ok($"Synced {allProducts.Count()} products to Elasticsearch.");
    }

    // 7. GET Single Product Details (with Reviews & Seller)
    [HttpGet("{id}")]
    [AllowAnonymous] // ✅ Already exists here, which is good
    public async Task<IActionResult> GetProductDetail(int id)
    {
        var product = await _repo.GetProductDetail(id);

        if (product == null)
            return NotFound("Product not found");

        return Ok(product);
    }
}