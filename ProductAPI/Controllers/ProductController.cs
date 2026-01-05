using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductAPI.Models;
using ProductAPI.Repositories;

namespace ProductAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Default: All endpoints require a valid token
public class ProductController : ControllerBase
{
    private readonly IProductRepository _repo;

    public ProductController(IProductRepository repo)
    {
        _repo = repo;
    }

    // 1. VIEW: Customers (and Admins) can view products
    // [Authorize] is inherited from the class, so any logged-in user (Customer/Admin) can access this.
    // If you want to force it strictly: [Authorize(Roles = "Customer,SuperAdmin,Admin")]
    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _repo.GetAllProducts();
        return Ok(products);
    }

    // 2. CREATE: Only Admin/SuperAdmin
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> CreateProduct(Product product)
    {
        var id = await _repo.CreateProduct(product);
        product.Id = id;
        return CreatedAtAction(nameof(GetProducts), new { id }, product);
    }

    // 3. UPDATE STOCK: Only Admin/SuperAdmin
    [HttpPut("stock/{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] int newStock)
    {
        await _repo.UpdateStock(id, newStock);
        return Ok($"Stock updated to {newStock} for Product {id}");
    }

    // 4. DELETE: Only Admin/SuperAdmin
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var affected = await _repo.DeleteProduct(id);
        if (affected == 0) return NotFound();
        return Ok("Product deleted");
    }
}