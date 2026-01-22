using Dapper;
using ProductAPI.Data;
using ProductAPI.Models;

namespace ProductAPI.Repositories;

public interface IWishlistRepository
{
    Task<IEnumerable<WishlistItem>> GetWishlistAsync(int userId);
    Task AddToWishlistAsync(int userId, int productId);
    Task RemoveFromWishlistAsync(int userId, int productId);
}

public class WishlistRepository : IWishlistRepository
{
    private readonly DapperContext _context;

    public WishlistRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<WishlistItem>> GetWishlistAsync(int userId)
    {
        // Efficient JOIN to get product details in one go
        var sql = @"
            SELECT 
                p.*, 
                w.created_at as AddedOn
            FROM wishlists w
            JOIN products p ON w.product_id = p.id
            WHERE w.user_id = @UserId
            ORDER BY w.created_at DESC";

        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<WishlistItem>(sql, new { UserId = userId });
    }

    public async Task AddToWishlistAsync(int userId, int productId)
    {
        // Idempotent insert: If it exists, do nothing.
        var sql = @"
            INSERT INTO wishlists (user_id, product_id)
            VALUES (@UserId, @ProductId)
            ON CONFLICT (user_id, product_id) DO NOTHING";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { UserId = userId, ProductId = productId });
    }

    public async Task RemoveFromWishlistAsync(int userId, int productId)
    {
        var sql = "DELETE FROM wishlists WHERE user_id = @UserId AND product_id = @ProductId";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { UserId = userId, ProductId = productId });
    }
}