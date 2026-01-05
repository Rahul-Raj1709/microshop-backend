namespace ProducerAPI.Models;

// Changed: OrderId -> ProductId, Removed 'Product' string name
public record OrderRequest(int UserId, int ProductId, int Quantity);