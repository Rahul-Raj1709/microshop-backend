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
                sale_price AS SalePrice,
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
        var sql = @"INSERT INTO products (name, category, description, price, sale_price, stock, seller_id) 
                    VALUES (@Name, @Category, @Description, @Price, @SalePrice, @Stock, @seller_id) 
                    RETURNING id";
        using var connection = _context.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, product);
    }

    public async Task<int> UpdateProduct(Product product)
    {
        var sql = @"UPDATE products 
                    SET name = @Name, category = @Category, description = @Description,
                        price = @Price, sale_price = @SalePrice, stock = @Stock 
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
        var sqlProduct = @"SELECT * FROM products WHERE id = @Id";

        var sqlReviews = @"
        SELECT reviewer_name AS ReviewerName, rating, feedback, created_at AS Date 
        FROM product_reviews 
        WHERE product_id = @Id 
        ORDER BY created_at DESC";

        using var connection = _context.CreateConnection();

        var product = await connection.QuerySingleOrDefaultAsync<ProductDetail>(sqlProduct, new { Id = id });

        if (product != null)
        {
            var reviews = await connection.QueryAsync<ProductReview>(sqlReviews, new { Id = id });
            product.Reviews = reviews.ToList();

            product.SellerName = "Seller #" + product.seller_id;
        }

        return product;
    }
}