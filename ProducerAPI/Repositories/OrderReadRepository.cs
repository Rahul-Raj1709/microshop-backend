using Dapper;
using ProducerAPI.Data;
using ProducerAPI.Models;

namespace ProducerAPI.Repositories;

public interface IOrderReadRepository
{
    Task<IEnumerable<OrderHistory>> GetOrdersByUserId(int userId);
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
        // Note: Mapping snake_case DB columns to PascalCase properties
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
}