using ConsumerWorker.Data;
using Dapper;

namespace ConsumerWorker.Repositories;

// ConsumerWorker/Repositories/OrderRepository.cs
public class OrderRepository
{
    private readonly DapperContext _context;

    public OrderRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<ProductInfo?> GetProductInfoAsync(int productId)
    {
        // Query CATALOG_DB
        var sql = "SELECT id, name, price, sale_price AS SalePrice, stock, version, seller_id AS SellerId FROM products WHERE id = @Id";
        using var connection = _context.CreateCatalogConnection();
        return await connection.QuerySingleOrDefaultAsync<ProductInfo>(sql, new { Id = productId });
    }

    public async Task FinalizeOrderAsync(int userId, int productId, string productName, int quantity, int oldVersion, int sellerId, decimal price, string shippingAddress)
    {
        // 1. DEDUCT STOCK (Catalog DB)
        using (var catalogConn = _context.CreateCatalogConnection())
        {
            var updateSql = @"
                UPDATE products 
                SET stock = stock - @Quantity, 
                    version = version + 1 
                WHERE id = @ProductId 
                  AND version = @OldVersion 
                  AND stock >= @Quantity";

            var rowsAffected = await catalogConn.ExecuteAsync(updateSql, new { Quantity = quantity, ProductId = productId, OldVersion = oldVersion });

            if (rowsAffected == 0)
                throw new Exception("Concurrency Conflict: Stock changed or insufficient.");
        }

        // 2. CREATE ORDER (Sales DB)
        // Note: If this fails, you technically have a 'Ghost' stock deduction. 
        // In a real app, you would publish a 'StockCompensate' event to Kafka to undo step 1.
        using (var salesConn = _context.CreateSalesConnection())
        {
            var totalAmount = price * quantity;
            var insertSql = @"
                INSERT INTO orders (user_id, product_id, product_name, quantity, status, seller_id, total_amount, shipping_address, created_at) 
                VALUES (@UserId, @ProductId, @Name, @Quantity, 'Paid & Completed', @SellerId, @TotalAmount, @ShippingAddress, NOW())";

            await salesConn.ExecuteAsync(insertSql, new
            {
                UserId = userId,
                ProductId = productId,
                Name = productName,
                Quantity = quantity,
                SellerId = sellerId,
                TotalAmount = totalAmount,
                ShippingAddress = shippingAddress
            });
        }
    }

    // Keeps working because it only touches Catalog DB
    public async Task UpdateProductReviewStats(int productId, int rating)
    {
        using var connection = _context.CreateCatalogConnection();
        var sql = @"UPDATE products SET review_count = review_count + 1, rating_sum = rating_sum + @Rating WHERE id = @Id";
        await connection.ExecuteAsync(sql, new { Id = productId, Rating = rating });
    }
}
public class ProductInfo
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public decimal? SalePrice { get; set; }
    public int Stock { get; set; }
    public int Version { get; set; }
    public int SellerId { get; set; }
}