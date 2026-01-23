using AuthAPI.Data;
using AuthAPI.Models;
using Dapper;

namespace AuthAPI.Repositories;

public interface IStoreRepository
{
    Task<Store?> GetStoreByUserId(int userId);
    Task CreateStore(Store store);
    Task UpdateStore(Store store);
    Task<bool> StoreNameExists(string storeName);
}

public class StoreRepository : IStoreRepository
{
    private readonly DapperContext _context;

    public StoreRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<Store?> GetStoreByUserId(int userId)
    {
        var sql = "SELECT * FROM stores WHERE user_id = @UserId";
        using var connection = _context.CreateConnection();
        // Dapper maps snake_case (user_id) to PascalCase (UserId) automatically with DefaultTypeMapMatchNames
        // typically, but explicit mapping or "AS" alias in SQL is safer if default mapping isn't set up.
        // For simplicity, we use explicit aliases to match C# properties.
        var secureSql = @"
            SELECT 
                user_id AS UserId, 
                store_name AS StoreName, 
                description, 
                logo_url AS LogoUrl, 
                banner_url AS BannerUrl, 
                support_email AS SupportEmail, 
                website_url AS WebsiteUrl,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM stores WHERE user_id = @UserId";

        return await connection.QuerySingleOrDefaultAsync<Store>(secureSql, new { UserId = userId });
    }

    public async Task CreateStore(Store store)
    {
        var sql = @"
            INSERT INTO stores (user_id, store_name, description, logo_url, banner_url, support_email, website_url)
            VALUES (@UserId, @StoreName, @Description, @LogoUrl, @BannerUrl, @SupportEmail, @WebsiteUrl)";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, store);
    }

    public async Task UpdateStore(Store store)
    {
        var sql = @"
            UPDATE stores 
            SET store_name = @StoreName, 
                description = @Description, 
                logo_url = @LogoUrl, 
                banner_url = @BannerUrl, 
                support_email = @SupportEmail, 
                website_url = @WebsiteUrl,
                updated_at = NOW()
            WHERE user_id = @UserId";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, store);
    }

    public async Task<bool> StoreNameExists(string storeName)
    {
        var sql = "SELECT COUNT(1) FROM stores WHERE store_name = @StoreName";
        using var connection = _context.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new { StoreName = storeName });
    }
}