using ConsumerWorker.Data;
using Dapper;

namespace ConsumerWorker.Repositories;

public class OrderRepository
{
    private readonly DapperContext _context;

    public OrderRepository(DapperContext context)
    {
        _context = context;
    }

    // 1. READ
    public async Task<ProductInfo> GetProductInfoAsync(int productId)
    {
        var sql = "SELECT id, name, price, stock, version, seller_id AS SellerId FROM products WHERE id = @Id";
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ProductInfo>(sql, new { Id = productId });
    }

    // 2. WRITE (Fixed: Uses ProductId for concurrency check instead of Name)
    public async Task FinalizeOrderAsync(int userId, int productId, string productName, int quantity, int oldVersion, int sellerId, decimal price, string shippingAddress)
    {
        using var connection = _context.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // A. Attempt Atomic Update (Optimistic Concurrency)
            // FIX: Changed 'WHERE name = @Name' to 'WHERE id = @ProductId' for safety.
            var updateSql = @"
                UPDATE products 
                SET stock = stock - @Quantity, 
                    version = version + 1 
                WHERE id = @ProductId 
                  AND version = @OldVersion 
                  AND stock >= @Quantity";

            var rowsAffected = await connection.ExecuteAsync(updateSql, new
            {
                Quantity = quantity,
                ProductId = productId, // Use the ID here
                OldVersion = oldVersion
            }, transaction);

            if (rowsAffected == 0)
            {
                // This means either the stock was too low OR the version changed (someone else bought it)
                throw new Exception("Concurrency Conflict: Stock changed or insufficient during payment.");
            }

            // B. Save Order
            var totalAmount = price * quantity;
            var insertSql = @"INSERT INTO orders (user_id, product_id, product_name, quantity, status, seller_id, total_amount, shipping_address, created_at) 
                  VALUES (@UserId, @ProductId, @Name, @Quantity, 'Paid & Completed', @SellerId, @TotalAmount, @ShippingAddress, NOW())";

            await connection.ExecuteAsync(insertSql, new
            {
                UserId = userId,
                ProductId = productId,
                Name = productName,
                Quantity = quantity,
                SellerId = sellerId,
                TotalAmount = totalAmount,
                ShippingAddress = shippingAddress
            }, transaction);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task UpdateProductReviewStats(int productId, int rating)
    {
        using var connection = _context.CreateConnection();
        var sql = @"
            UPDATE products 
            SET review_count = review_count + 1,
                rating_sum = rating_sum + @Rating
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, new { Id = productId, Rating = rating });
    }
}

public class ProductInfo
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int Version { get; set; }
    public int SellerId { get; set; }
}