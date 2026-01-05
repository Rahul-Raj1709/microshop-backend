using Dapper;
using ProductAPI.Data;
using ProductAPI.Models;

namespace ProductAPI.Repositories;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllProducts();

    Task<int> CreateProduct(Product product);

    Task<int> DeleteProduct(int id);

    // NEW: Allows updating the stock quantity
    Task UpdateStock(int id, int quantity);
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

    public async Task<int> CreateProduct(Product product)
    {
        var sql = "INSERT INTO products (name, price, stock) VALUES (@Name, @Price, @Stock) RETURNING id";
        using var connection = _context.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, product);
    }

    public async Task UpdateStock(int id, int quantity)
    {
        // This replaces the stock count directly (e.g., set stock to 50)
        var sql = "UPDATE products SET stock = @Stock WHERE id = @Id";
        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { Stock = quantity, Id = id });
    }
    public async Task<int> DeleteProduct(int id)
    {
        var sql = "DELETE FROM products WHERE id = @Id";
        using var connection = _context.CreateConnection();
        return await connection.ExecuteAsync(sql, new { Id = id });
    }
}