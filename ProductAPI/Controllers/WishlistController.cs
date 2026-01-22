using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductAPI.Models;
using ProductAPI.Repositories;
using System.Security.Claims;

namespace ProductAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Customer")]
public class WishlistController : ControllerBase
{
    private readonly IWishlistRepository _repo;

    public WishlistController(IWishlistRepository repo)
    {
        _repo = repo;
    }

    // Helper to extract UserID safely from JWT
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userid") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out int id) ? id : 0;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyWishlist()
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var items = await _repo.GetWishlistAsync(userId);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> AddToWishlist([FromBody] WishlistRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        await _repo.AddToWishlistAsync(userId, request.ProductId);
        return Ok(new { Message = "Product added to wishlist" });
    }

    [HttpDelete("{productId}")]
    public async Task<IActionResult> RemoveFromWishlist(int productId)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        await _repo.RemoveFromWishlistAsync(userId, productId);
        return Ok(new { Message = "Product removed from wishlist" });
    }
}