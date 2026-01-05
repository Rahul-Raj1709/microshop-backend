using Microsoft.AspNetCore.Mvc;
using PaymentAPI.Models;

namespace PaymentAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(ILogger<PaymentController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public IActionResult ProcessPayment([FromBody] PaymentRequest request)
    {
        _logger.LogInformation($"Processing payment of ${request.Amount} for User {request.UserId}");

        // MOCK LOGIC:
        if (request.Amount <= 0)
            return BadRequest("Invalid amount");

        // Simulate Card Decline for high amounts
        if (request.Amount > 5000)
        {
            _logger.LogWarning("Payment Declined: Exceeds limit.");
            return BadRequest("Payment Declined: Amount exceeds card limit.");
        }

        // Simulate Success
        return Ok(new
        {
            Status = "Authorized",
            TransactionId = Guid.NewGuid(),
            Message = "Payment successful"
        });
    }
}