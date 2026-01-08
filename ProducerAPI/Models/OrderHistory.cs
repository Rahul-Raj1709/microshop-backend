namespace ProducerAPI.Models;

public class OrderHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProductId { get; set; } // Added
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }

    // New Fields for Review
    public int Rating { get; set; }
    public string Feedback { get; set; } = string.Empty;
}

// Request Model for submitting feedback
public class OrderFeedbackRequest
{
    public int Rating { get; set; }
    public string Feedback { get; set; } = string.Empty;
}
public class OrderDetail : OrderHistory
{
    public decimal TotalAmount { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string SellerEmail { get; set; } = string.Empty;
}