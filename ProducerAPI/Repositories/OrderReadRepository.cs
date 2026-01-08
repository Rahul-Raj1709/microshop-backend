using Dapper;
using ProducerAPI.Data;
using ProducerAPI.Models;

namespace ProducerAPI.Repositories;

public interface IOrderReadRepository
{
    // Updated signature for Pagination & Filtering
    Task<PagedOrderResult> GetOrdersByUserId(int userId, int page, int pageSize, int? year);
    Task<OrderDetail?> GetOrderDetails(int orderId);
    Task<int?> AddFeedback(int orderId, int rating, string feedback);
}

public class PagedOrderResult
{
    public IEnumerable<OrderHistory> Orders { get; set; } = new List<OrderHistory>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class OrderReadRepository : IOrderReadRepository
{
    private readonly DapperContext _context;

    public OrderReadRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<PagedOrderResult> GetOrdersByUserId(int userId, int page, int pageSize, int? year)
    {
        var offset = (page - 1) * pageSize;
        var builder = new SqlBuilder();

        // Base Query
        var template = builder.AddTemplate(@"
            SELECT COUNT(*) FROM orders /**where**/;
            
            SELECT 
                id, user_id AS UserId, product_id AS ProductId, product_name AS ProductName, 
                quantity, status, created_at AS CreatedAt, total_amount AS TotalAmount,
                rating, feedback
            FROM orders 
            /**where**/ 
            ORDER BY created_at DESC 
            LIMIT @PageSize OFFSET @Offset");

        // Dynamic Filters
        builder.Where("user_id = @UserId", new { UserId = userId });

        if (year.HasValue)
        {
            builder.Where("EXTRACT(YEAR FROM created_at) = @Year", new { Year = year });
        }

        using var connection = _context.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(template.RawSql,
            new { UserId = userId, PageSize = pageSize, Offset = offset, Year = year });

        var totalCount = await multi.ReadFirstAsync<int>();
        var orders = await multi.ReadAsync<OrderHistory>();

        return new PagedOrderResult
        {
            Orders = orders,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<OrderDetail?> GetOrderDetails(int orderId)
    {
        // ... (Keep existing implementation, just add rating/feedback to SELECT if needed)
        var sql = @"
            SELECT 
                o.id, o.user_id AS UserId, o.product_name AS ProductName, 
                o.quantity, o.status, o.created_at AS CreatedAt,
                o.total_amount AS TotalAmount, o.rating, o.feedback,
                u.name AS SellerName, u.email AS SellerEmail
            FROM orders o
            LEFT JOIN users u ON o.seller_id = u.id
            WHERE o.id = @Id";

        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<OrderDetail>(sql, new { Id = orderId });
    }

    // New Method: Submit Feedback
    public async Task<int?> AddFeedback(int orderId, int rating, string feedback)
    {
        // Update the order and return the product_id so we can send it to Kafka
        var sql = @"
            UPDATE orders 
            SET rating = @Rating, feedback = @Feedback 
            WHERE id = @Id 
            RETURNING product_id";

        using var connection = _context.CreateConnection();
        return await connection.ExecuteScalarAsync<int?>(sql, new { Id = orderId, Rating = rating, Feedback = feedback });
    }
}