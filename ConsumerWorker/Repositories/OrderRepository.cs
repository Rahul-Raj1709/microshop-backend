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

    // 1. READ (Get Stock, Price, AND Version)
    public async Task<ProductInfo> GetProductInfoAsync(int productId)
    {
        // Added seller_id
        var sql = "SELECT id, name, price, stock, version, seller_id AS SellerId FROM products WHERE id = @Id";
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ProductInfo>(sql, new { Id = productId });
    }
    // 2. WRITE (Update ONLY if version matches)
    public async Task FinalizeOrderAsync(int userId, string productName, int quantity, int oldVersion, int sellerId, decimal price, string shippingAddress)
    {
        using var connection = _context.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // A. Attempt Atomic Update
            // We check 'version = @OldVersion' to ensure no one touched it while we were paying
            var updateSql = @"
                UPDATE products 
                SET stock = stock - @Quantity, 
                    version = version + 1 
                WHERE name = @Name 
                  AND version = @OldVersion 
                  AND stock >= @Quantity"; // Double check stock just in case

            var rowsAffected = await connection.ExecuteAsync(updateSql, new
            {
                Quantity = quantity,
                Name = productName,
                OldVersion = oldVersion
            }, transaction);

            // B. Concurrency Check
            if (rowsAffected == 0)
            {
                // This means someone else changed the stock or version!
                throw new Exception("Concurrency Conflict: Stock changed during payment.");
            }

            // C. Save Order
            var totalAmount = price * quantity;
            var insertSql = @"INSERT INTO orders (user_id, product_name, quantity, status, seller_id, total_amount, shipping_address, created_at) 
                  VALUES (@UserId, @Name, @Quantity, 'Paid & Completed', @SellerId, @TotalAmount, @ShippingAddress, NOW())";
            await connection.ExecuteAsync(insertSql, new
            {
                UserId = userId,
                Name = productName,
                Quantity = quantity,
                SellerId = sellerId,
                TotalAmount = totalAmount,
                ShippingAddress = shippingAddress // <--- ADD THIS
            }, transaction); transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw; // Re-throw so the Worker knows it failed
        }
    }
}

// Update the Model to include Version
public class ProductInfo
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int Version { get; set; }
    public int SellerId { get; set; } // <--- New Field
}