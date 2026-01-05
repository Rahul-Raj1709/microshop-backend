using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductAPI.Models;
using ProductAPI.Repositories;
using System.Security.Claims;

namespace ProductAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ProductController : ControllerBase
{
    private readonly IProductRepository _repo;

    public ProductController(IProductRepository repo)
    {
        _repo = repo;
    }

    // Helper: Extract User ID from JWT Token
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userid") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("User ID not found in token");
    }

    // 1. GET: Supports Pagination & Role-based filtering
    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        // Basic validation
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        int? filterSellerId = null;

        // If Admin/SuperAdmin, filter to show only their own products
        if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
        {
            filterSellerId = GetCurrentUserId();
        }

        var pagedResult = await _repo.GetProducts(filterSellerId, page, pageSize);

        return Ok(pagedResult);
    }

    // 2. CREATE: Admin Only
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> CreateProduct(Product product)
    {
        // Enforce seller_id from the token
        product.seller_id = GetCurrentUserId();

        var id = await _repo.CreateProduct(product);
        product.Id = id;

        // Return 201 Created
        return CreatedAtAction(nameof(GetProducts), new { id }, product);
    }

    // 3. UPDATE: Admin Only (Full Update)
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product product)
    {
        product.Id = id;

        // Enforce seller_id from the token so they cannot update others' products
        product.seller_id = GetCurrentUserId();

        var affected = await _repo.UpdateProduct(product);

        if (affected == 0)
            return NotFound("Product not found or you do not have permission to edit it.");

        return Ok("Product updated successfully");
    }

    // 4. DELETE: Admin Only
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        int currentUserId = GetCurrentUserId();

        var affected = await _repo.DeleteProduct(id, currentUserId);

        if (affected == 0)
            return NotFound("Product not found or you do not have permission to delete it.");

        return Ok("Product deleted");
    }
}