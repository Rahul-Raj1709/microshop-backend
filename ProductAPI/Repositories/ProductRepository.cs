using Dapper;
using ProductAPI.Data;
using ProductAPI.Models;

namespace ProductAPI.Repositories;

public interface IProductRepository
{
    Task<PagedResult<Product>> GetProducts(int? sellerId, string? category, int pageNumber, int pageSize);
    Task<IEnumerable<Product>> GetAllProducts();
    Task<int> CreateProduct(Product product);
    Task<int> DeleteProduct(int id, int sellerId);
    Task<int> UpdateProduct(Product product);
    Task<Product?> GetProductById(int id);
    Task<ProductDetail?> GetProductDetail(int id);
}

public class ProductRepository : IProductRepository
{
    private readonly DapperContext _context;

    public ProductRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Product>> GetAllProducts()
    {
        var sql = "SELECT * FROM products";
        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<Product>(sql);
    }

    public async Task<PagedResult<Product>> GetProducts(int? sellerId, string? category, int pageNumber, int pageSize)
    {
        using var connection = _context.CreateConnection();
        int offset = (pageNumber - 1) * pageSize;
        var builder = new SqlBuilder();

        // UPDATED SQL: Select average_rating and review_count
        var selector = builder.AddTemplate(@"
            SELECT count(*) FROM products /**where**/;
            
            SELECT 
                id, name, category, description, price, stock, seller_id,
                average_rating AS AverageRating,
                review_count AS ReviewCount
            FROM products 
            /**where**/ 
            ORDER BY id 
            LIMIT @PageSize OFFSET @Offset;
        ", new { PageSize = pageSize, Offset = offset });

        if (sellerId.HasValue)
            builder.Where("seller_id = @SellerId", new { SellerId = sellerId });

        if (!string.IsNullOrEmpty(category))
            builder.Where("category = @Category", new { Category = category });

        using var multi = await connection.QueryMultipleAsync(selector.RawSql, selector.Parameters);
        var totalCount = await multi.ReadFirstAsync<int>();
        var products = await multi.ReadAsync<Product>();

        return new PagedResult<Product>
        {
            Items = products,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    // ... (Keep CreateProduct, UpdateProduct, DeleteProduct, GetProductDetail, GetProductById as they were) ...
    public async Task<int> CreateProduct(Product product)
    {
        var sql = @"INSERT INTO products (name, category, description, price, stock, seller_id) 
                    VALUES (@Name, @Category, @Description, @Price, @Stock, @seller_id) 
                    RETURNING id";
        using var connection = _context.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, product);
    }

    public async Task<int> UpdateProduct(Product product)
    {
        var sql = @"UPDATE products 
                    SET name = @Name, category = @Category, description = @Description,
                        price = @Price, stock = @Stock 
                    WHERE id = @Id AND seller_id = @seller_id";
        using var connection = _context.CreateConnection();
        return await connection.ExecuteAsync(sql, product);
    }

    public async Task<int> DeleteProduct(int id, int sellerId)
    {
        var sql = "DELETE FROM products WHERE id = @Id AND seller_id = @SellerId";
        using var connection = _context.CreateConnection();
        return await connection.ExecuteAsync(sql, new { Id = id, SellerId = sellerId });
    }

    public async Task<Product?> GetProductById(int id)
    {
        var sql = "SELECT * FROM products WHERE id = @Id";
        using var connection = _context.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Product>(sql, new { Id = id });
    }

    public async Task<ProductDetail?> GetProductDetail(int id)
    {
        // Keep your existing implementation here
        var sql = @"SELECT p.*, u.name AS SellerName, u.email AS SellerEmail FROM products p LEFT JOIN users u ON p.seller_id = u.id WHERE p.id = @Id;
                    SELECT u.name AS ReviewerName, o.rating, o.feedback, o.created_at AS Date FROM orders o LEFT JOIN users u ON o.user_id = u.id WHERE o.product_id = @Id AND o.rating > 0 ORDER BY o.created_at DESC;";
        using var connection = _context.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(sql, new { Id = id });
        var product = await multi.ReadSingleOrDefaultAsync<ProductDetail>();
        if (product != null)
        {
            var reviews = (await multi.ReadAsync<ProductReview>()).ToList();
            product.Reviews = reviews;
            // Note: Now we can trust the columns in 'p' (AverageRating) instead of calculating manually, 
            // but for ProductDetail page, calculating manually from the list is also fine.
            if (reviews.Any())
            {
                product.TotalReviews = reviews.Count;
                product.AverageRating = Math.Round(reviews.Average(r => r.Rating), 1);
            }
        }
        return product;
    }
}