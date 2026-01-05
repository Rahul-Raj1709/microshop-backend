using Dapper;
using ProductAPI.Data;
using ProductAPI.Models;

namespace ProductAPI.Repositories;

public interface IProductRepository
{
    Task<PagedResult<Product>> GetProducts(int? sellerId, int pageNumber, int pageSize);
    Task<int> CreateProduct(Product product);
    Task<int> DeleteProduct(int id, int sellerId);
    Task<int> UpdateProduct(Product product);
}

public class ProductRepository : IProductRepository
{
    private readonly DapperContext _context;

    public ProductRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<Product>> GetProducts(int? sellerId, int pageNumber, int pageSize)
    {
        using var connection = _context.CreateConnection();

        // 1. Calculate Offset for Pagination
        int offset = (pageNumber - 1) * pageSize;

        // 2. Build SQL using SqlBuilder to handle the optional WHERE clause
        var builder = new SqlBuilder();

        // Define the template: One query for Count, one for Data
        var selector = builder.AddTemplate(@"
            SELECT count(*) FROM products /**where**/;
            SELECT * FROM products /**where**/ ORDER BY id LIMIT @PageSize OFFSET @Offset;
        ", new { PageSize = pageSize, Offset = offset });

        // Apply dynamic filter if the user is an Admin (sellerId is not null)
        if (sellerId.HasValue)
        {
            builder.Where("seller_id = @SellerId", new { SellerId = sellerId });
        }

        // 3. Execute both queries in one go
        using var multi = await connection.QueryMultipleAsync(selector.RawSql, selector.Parameters);

        var totalCount = await multi.ReadFirstAsync<int>();
        var products = await multi.ReadAsync<Product>();

        // 4. Return the combined PagedResult
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
        // Uses @seller_id to match the C# property "product.seller_id"
        var sql = "INSERT INTO products (name, price, stock, seller_id) VALUES (@Name, @Price, @Stock, @seller_id) RETURNING id";

        using var connection = _context.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, product);
    }

    public async Task<int> UpdateProduct(Product product)
    {
        // Validates ownership: WHERE seller_id = @seller_id
        var sql = @"UPDATE products 
                    SET name = @Name, price = @Price, stock = @Stock 
                    WHERE id = @Id AND seller_id = @seller_id";

        using var connection = _context.CreateConnection();
        return await connection.ExecuteAsync(sql, product);
    }

    public async Task<int> DeleteProduct(int id, int sellerId)
    {
        // Validates ownership
        var sql = "DELETE FROM products WHERE id = @Id AND seller_id = @SellerId";

        using var connection = _context.CreateConnection();
        return await connection.ExecuteAsync(sql, new { Id = id, SellerId = sellerId });
    }
}