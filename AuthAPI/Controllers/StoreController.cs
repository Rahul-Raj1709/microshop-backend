using AuthAPI.Models;
using AuthAPI.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StoreController : ControllerBase
{
    private readonly IStoreRepository _repo;

    public StoreController(IStoreRepository repo)
    {
        _repo = repo;
    }

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirst("userid")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idStr, out int id) ? id : 0;
    }

    // GET: api/store/me
    // Used by the Seller Dashboard to load their own profile
    [HttpGet("me")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetMyStore()
    {
        var userId = GetCurrentUserId();
        var store = await _repo.GetStoreByUserId(userId);

        if (store == null) return NoContent(); // Store not set up yet

        return Ok(store);
    }

    // GET: api/store/{userId}
    // Public endpoint for Customers (and ProductAPI) to see Seller details
    [HttpGet("{userId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStorePublicProfile(int userId)
    {
        var store = await _repo.GetStoreByUserId(userId);
        if (store == null) return NotFound("Store profile not found.");
        return Ok(store);
    }

    // POST: api/store
    // Create or Update Store Profile
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpsertStore([FromBody] StoreRequest request)
    {
        var userId = GetCurrentUserId();
        var existingStore = await _repo.GetStoreByUserId(userId);

        // Check if name is taken by ANOTHER user
        if (await _repo.StoreNameExists(request.StoreName))
        {
            // If it exists, ensure it belongs to THIS user (for updates)
            if (existingStore == null || existingStore.StoreName != request.StoreName)
            {
                return BadRequest("Store name is already taken.");
            }
        }

        if (existingStore == null)
        {
            // Create New
            var newStore = new Store
            {
                UserId = userId,
                StoreName = request.StoreName,
                Description = request.Description,
                LogoUrl = request.LogoUrl,
                BannerUrl = request.BannerUrl,
                SupportEmail = request.SupportEmail,
                WebsiteUrl = request.WebsiteUrl
            };
            await _repo.CreateStore(newStore);
            return CreatedAtAction(nameof(GetMyStore), newStore);
        }
        else
        {
            // Update Existing
            existingStore.StoreName = request.StoreName;
            existingStore.Description = request.Description;
            existingStore.LogoUrl = request.LogoUrl;
            existingStore.BannerUrl = request.BannerUrl;
            existingStore.SupportEmail = request.SupportEmail;
            existingStore.WebsiteUrl = request.WebsiteUrl;

            await _repo.UpdateStore(existingStore);
            return Ok(existingStore);
        }
    }
}