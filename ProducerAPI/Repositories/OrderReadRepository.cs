using Dapper;
using ProducerAPI.Data;
using ProducerAPI.Models;

namespace ProducerAPI.Repositories;

public interface IOrderReadRepository
{
    Task<IEnumerable<OrderHistory>> GetOrdersByUserId(int userId);
    // Add this new method
    Task<OrderDetail?> GetOrderDetails(int orderId);
}

public class OrderReadRepository : IOrderReadRepository
{
    private readonly DapperContext _context;

    public OrderReadRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<OrderHistory>> GetOrdersByUserId(int userId)
    {
        var sql = @"
            SELECT 
                id, 
                user_id AS UserId, 
                product_name AS ProductName, 
                quantity, 
                status, 
                created_at AS CreatedAt 
            FROM orders 
            WHERE user_id = @UserId 
            ORDER BY created_at DESC";

        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<OrderHistory>(sql, new { UserId = userId });
    }

    // Implement the new method
    public async Task<OrderDetail?> GetOrderDetails(int orderId)
    {
        var sql = @"
            SELECT 
                o.id, 
                o.user_id AS UserId, 
                o.product_name AS ProductName, 
                o.quantity, 
                o.status, 
                o.created_at AS CreatedAt,
                o.total_amount AS TotalAmount,
                u.name AS SellerName,
                u.email AS SellerEmail
            FROM orders o
            LEFT JOIN users u ON o.seller_id = u.id
            WHERE o.id = @Id";

        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<OrderDetail>(sql, new { Id = orderId });
    }
}