namespace ProducerAPI.Models;

public record OrderRequest(int UserId, int ProductId, int Quantity, string ShippingAddress);