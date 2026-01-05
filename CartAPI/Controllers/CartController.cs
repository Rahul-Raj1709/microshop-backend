using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Text.Json;

namespace CartAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CartController : ControllerBase
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IHttpClientFactory _httpClientFactory;

    public CartController(IConnectionMultiplexer redis, IHttpClientFactory httpClientFactory)
    {
        _redis = redis;
        _httpClientFactory = httpClientFactory;
    }

    // GET /api/cart
    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = User.FindFirst("userid")?.Value;
        var db = _redis.GetDatabase();
        var cartData = await db.StringGetAsync($"cart:{userId}");

        if (cartData.IsNullOrEmpty) return Ok(new List<CartItem>());
        return Ok(JsonSerializer.Deserialize<List<CartItem>>(cartData.ToString()));
    }

    // POST /api/cart
    [HttpPost]
    public async Task<IActionResult> AddToCart(CartItem item)
    {
        var userId = User.FindFirst("userid")?.Value;
        var db = _redis.GetDatabase();
        var key = $"cart:{userId}";

        var cartData = await db.StringGetAsync(key);
        var cart = cartData.IsNullOrEmpty
            ? new List<CartItem>()
            : JsonSerializer.Deserialize<List<CartItem>>(cartData.ToString());

        // Check if item exists, update quantity
        var existing = cart.FirstOrDefault(x => x.Product == item.Product);
        if (existing != null) existing.Quantity += item.Quantity;
        else cart.Add(item);

        await db.StringSetAsync(key, JsonSerializer.Serialize(cart), TimeSpan.FromDays(30));
        return Ok("Item added to cart");
    }

    // POST /api/cart/checkout
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout()
    {
        var userIdStr = User.FindFirst("userid")?.Value;
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
        int userId = int.Parse(userIdStr); // We need the int UserID now

        var db = _redis.GetDatabase();
        var key = $"cart:{userIdStr}";
        var cartData = await db.StringGetAsync(key);

        if (cartData.IsNullOrEmpty) return BadRequest("Cart is empty");
        var cart = JsonSerializer.Deserialize<List<CartItem>>(cartData.ToString());

        var client = _httpClientFactory.CreateClient("ProducerClient");

        // Forward Token Logic (Keep existing code)
        var tokenHeader = Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(tokenHeader))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", tokenHeader);
        }

        // --- NEW MAPPING ---
        var orderRequests = cart.Select(c => new
        {
            UserId = userId,       // Explicitly passing UserID
            ProductId = c.ProductId, // Using ID, not Name
            Quantity = c.Quantity
        }).ToList();

        var response = await client.PostAsJsonAsync("Order/batch", orderRequests);

        if (!response.IsSuccessStatusCode) return BadRequest("Order Failed");

        await db.KeyDeleteAsync(key);
        return Ok($"Checkout successful.");
    }
}

public class CartItem
{
    public int ProductId { get; set; } // <--- ADDED
    public string Product { get; set; } // Kept for UI display purposes
    public int Quantity { get; set; }
}