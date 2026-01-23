using Dapper;
using ProducerAPI.Data;
using ProducerAPI.Models;

namespace ProducerAPI.Repositories;

public class DashboardRepository
{
    private readonly DapperContext _context;

    public DashboardRepository(DapperContext context)
    {
        _context = context;
    }

    // Private DTO to handle the metrics query safely
    private class DashboardMetrics
    {
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
    }

    // ProducerAPI/Repositories/DashboardRepository.cs

    public async Task<DashboardStats> GetStatsAsync(int? sellerId)
    {
        using var connection = _context.CreateConnection();
        var stats = new DashboardStats();
        string whereClause = sellerId.HasValue ? "WHERE seller_id = @SellerId" : "";

        // 1. Metrics from ORDERS table only
        var metricsSql = $@"
        SELECT 
            COALESCE(SUM(total_amount), 0) as Revenue,
            COUNT(*) as Orders
        FROM orders {whereClause}";

        var metrics = await connection.QueryFirstAsync<dynamic>(metricsSql, new { SellerId = sellerId });
        stats.TotalRevenue = (decimal)metrics.revenue;
        stats.TotalOrders = (int)metrics.orders;

        // We can no longer count TotalProducts via SQL because the table isn't here.
        // Set to 0 or fetch via HTTP Client from ProductAPI.
        stats.TotalProducts = 0;

        // 2. Top Products (We can still do this because orders table has product_name!)
        var topSql = $@"
        SELECT 
            product_name as Name, 
            SUM(quantity) as Sales, 
            COALESCE(SUM(total_amount), 0) as Revenue
        FROM orders
        {whereClause}
        GROUP BY product_name
        ORDER BY Revenue DESC
        LIMIT 5";

        stats.TopProducts = (await connection.QueryAsync<TopProductDto>(topSql, new { SellerId = sellerId })).ToList();

        // 3. Recent Orders (Works fine, removing User Join if it existed)
        var recentSql = $@"
        SELECT id, user_id::text as Customer, product_name as Product, total_amount as Amount, status
        FROM orders {whereClause} ORDER BY created_at DESC LIMIT 5";

        stats.RecentOrders = (await connection.QueryAsync<RecentOrderDto>(recentSql, new { SellerId = sellerId })).ToList();

        return stats;
    }
}