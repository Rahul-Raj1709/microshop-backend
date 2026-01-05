using Dapper;
using ProductAPI.Data;
using ProductAPI.Models;

namespace ProductAPI.Repositories;

public interface IProductRepository
{
    // Updated signature to accept category
    Task<PagedResult<Product>> GetProducts(int? sellerId, string? category, int pageNumber, int pageSize);
    Task<IEnumerable<Product>> GetAllProducts();
    Task<int> CreateProduct(Product product);
    Task<int> DeleteProduct(int id, int sellerId);
    Task<int> UpdateProduct(Product product);
    Task<Product?> GetProductById(int id); // Added helper for sync
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
        // Simple query to fetch everything without filters or limits
        var sql = "SELECT * FROM products";

        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<Product>(sql);
    }

    public async Task<PagedResult<Product>> GetProducts(int? sellerId, string? category, int pageNumber, int pageSize)
    {
        using var connection = _context.CreateConnection();
        int offset = (pageNumber - 1) * pageSize;
        var builder = new SqlBuilder();

        // Template
        var selector = builder.AddTemplate(@"
            SELECT count(*) FROM products /**where**/;
            SELECT * FROM products /**where**/ ORDER BY id LIMIT @PageSize OFFSET @Offset;
        ", new { PageSize = pageSize, Offset = offset });

        // Filter by Seller
        if (sellerId.HasValue)
            builder.Where("seller_id = @SellerId", new { SellerId = sellerId });

        // Filter by Category (New)
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

    public async Task<int> CreateProduct(Product product)
    {
        // Added Category and Description
        var sql = @"INSERT INTO products (name, category, description, price, stock, seller_id) 
                    VALUES (@Name, @Category, @Description, @Price, @Stock, @seller_id) 
                    RETURNING id";

        using var connection = _context.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, product);
    }

    public async Task<int> UpdateProduct(Product product)
    {
        // Added Category and Description
        var sql = @"UPDATE products 
                    SET name = @Name, 
                        category = @Category,
                        description = @Description,
                        price = @Price, 
                        stock = @Stock 
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
}