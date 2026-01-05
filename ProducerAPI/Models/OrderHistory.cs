namespace ProducerAPI.Models;

public class OrderHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ProductName { get; set; } = string.Empty; // Maps to product_name column
    public int Quantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } // Maps to created_at
}