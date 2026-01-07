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

    // POST /api/cart (Add or Increment)
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

        var existing = cart.FirstOrDefault(x => x.ProductId == item.ProductId);
        if (existing != null)
        {
            existing.Quantity += item.Quantity;
        }
        else
        {
            cart.Add(item);
        }

        await db.StringSetAsync(key, JsonSerializer.Serialize(cart), TimeSpan.FromDays(30));
        return Ok("Item added to cart");
    }

    // PUT /api/cart (Update specific item quantity)
    [HttpPut]
    public async Task<IActionResult> UpdateCartItem(CartItem item)
    {
        var userId = User.FindFirst("userid")?.Value;
        var db = _redis.GetDatabase();
        var key = $"cart:{userId}";

        var cartData = await db.StringGetAsync(key);
        if (cartData.IsNullOrEmpty) return NotFound("Cart is empty");

        var cart = JsonSerializer.Deserialize<List<CartItem>>(cartData.ToString());
        var existing = cart.FirstOrDefault(x => x.ProductId == item.ProductId);

        if (existing == null) return NotFound("Item not found in cart");

        if (item.Quantity > 0)
        {
            existing.Quantity = item.Quantity;
        }
        else
        {
            cart.Remove(existing);
        }

        await db.StringSetAsync(key, JsonSerializer.Serialize(cart), TimeSpan.FromDays(30));
        return Ok("Cart updated");
    }

    // DELETE /api/cart/{productId} (Remove item completely)
    [HttpDelete("{productId}")]
    public async Task<IActionResult> RemoveFromCart(int productId)
    {
        var userId = User.FindFirst("userid")?.Value;
        var db = _redis.GetDatabase();
        var key = $"cart:{userId}";

        var cartData = await db.StringGetAsync(key);
        if (cartData.IsNullOrEmpty) return NotFound("Cart is empty");

        var cart = JsonSerializer.Deserialize<List<CartItem>>(cartData.ToString());
        var itemToRemove = cart.FirstOrDefault(x => x.ProductId == productId);

        if (itemToRemove == null) return NotFound("Item not found in cart");

        cart.Remove(itemToRemove);

        if (cart.Count > 0)
            await db.StringSetAsync(key, JsonSerializer.Serialize(cart), TimeSpan.FromDays(30));
        else
            await db.KeyDeleteAsync(key);

        return Ok("Item removed from cart");
    }

    // POST /api/cart/checkout
    // UPDATED: Now accepts CheckoutRequest body to capture shippingAddress
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        var userIdStr = User.FindFirst("userid")?.Value;
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
        int userId = int.Parse(userIdStr);

        var db = _redis.GetDatabase();
        var key = $"cart:{userIdStr}";
        var cartData = await db.StringGetAsync(key);

        if (cartData.IsNullOrEmpty) return BadRequest("Cart is empty");
        var cart = JsonSerializer.Deserialize<List<CartItem>>(cartData.ToString());

        var client = _httpClientFactory.CreateClient("ProducerClient");

        var tokenHeader = Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(tokenHeader))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", tokenHeader);
        }

        // UPDATED: Include ShippingAddress in the payload sent to ProducerAPI
        var orderRequests = cart.Select(c => new
        {
            UserId = userId,
            ProductId = c.ProductId,
            Quantity = c.Quantity,
            ShippingAddress = request.ShippingAddress // <--- PASSED HERE
        }).ToList();

        var response = await client.PostAsJsonAsync("Order/batch", orderRequests);

        if (!response.IsSuccessStatusCode) return BadRequest("Order Failed");

        await db.KeyDeleteAsync(key);
        return Ok($"Checkout successful.");
    }
}

public class CartItem
{
    public int ProductId { get; set; }
    public string Product { get; set; }
    public int Quantity { get; set; }
}

// NEW CLASS for request body
public class CheckoutRequest
{
    public string ShippingAddress { get; set; }
}