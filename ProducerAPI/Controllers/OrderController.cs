using Confluent.Kafka;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProducerAPI.Models;
using ProducerAPI.Repositories;
using System.Security.Claims;
using System.Text.Json;

namespace ProducerAPI.Controllers;

[Authorize(Roles = "Customer,Admin")]
[ApiController]
[Route("[controller]")]
public class OrderController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ILogger<OrderController> _logger;
    private readonly IOrderReadRepository _repo;

    public OrderController(IConfiguration config, ILogger<OrderController> logger, IOrderReadRepository repo)
    {
        _config = config;
        _logger = logger;
        _repo = repo;
    }

    // --- GET Order History ---
    [HttpGet("history")]
    public async Task<IActionResult> GetOrderHistory()
    {
        var userIdClaim = User.FindFirst("userid") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized("User ID not found.");

        var orders = await _repo.GetOrdersByUserId(userId);
        return Ok(orders);
    }

    // --- GET Single Order Details ---
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderDetails(int id)
    {
        var userIdClaim = User.FindFirst("userid") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized("User ID not found.");

        var order = await _repo.GetOrderDetails(id);

        if (order == null)
            return NotFound("Order not found");

        // Optional: specific security check to ensure the user owns the order
        if (order.UserId != userId)
            return Forbid();

        return Ok(order);
    }

    // --- POST Single Order ---
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] OrderRequest order)
    {
        // Validate User
        var userIdClaim = User.FindFirst("userid") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized("User ID not found.");

        var config = new ProducerConfig { BootstrapServers = _config["Kafka:BootstrapServers"] };
        using var producer = new ProducerBuilder<Null, string>(config).Build();

        try
        {
            // FIX: Map using ProductId and the correct UserId
            var kafkaMessage = new
            {
                UserId = userId,
                ProductId = order.ProductId,
                Quantity = order.Quantity,
                ShippingAddress = order.ShippingAddress // <--- ADD THIS
            };
            var messageJson = JsonSerializer.Serialize(kafkaMessage);
            await producer.ProduceAsync(_config["Kafka:Topic"], new Message<Null, string> { Value = messageJson });

            return Ok(new { Status = "Order Placed" });
        }
        catch (ProduceException<Null, string> e)
        {
            return StatusCode(500, $"Delivery failed: {e.Error.Reason}");
        }
    }

    // --- POST Batch Order (Used by CartAPI) ---
    [HttpPost("batch")]
    public async Task<IActionResult> CreateOrderBatch([FromBody] List<OrderRequest> orders)
    {
        // Validate User
        var userIdClaim = User.FindFirst("userid") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized("User ID not found.");

        var config = new ProducerConfig { BootstrapServers = _config["Kafka:BootstrapServers"] };
        using var producer = new ProducerBuilder<Null, string>(config).Build();

        try
        {
            foreach (var order in orders)
            {
                var kafkaMessage = new
                {
                    UserId = userId,
                    ProductId = order.ProductId,
                    Quantity = order.Quantity,
                    ShippingAddress = order.ShippingAddress // <--- ADD THIS LINE
                };

                var messageJson = JsonSerializer.Serialize(kafkaMessage);
                await producer.ProduceAsync(_config["Kafka:Topic"], new Message<Null, string> { Value = messageJson });
            }
            _logger.LogInformation($"Batch processed: {orders.Count} orders sent to Kafka.");
            return Ok(new { Status = "Batch Sent", Count = orders.Count });
        }
        catch (ProduceException<Null, string> e)
        {
            return StatusCode(500, $"Delivery failed: {e.Error.Reason}");
        }
    }
}