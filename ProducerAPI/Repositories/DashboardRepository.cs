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

    public async Task<DashboardStats> GetStatsAsync(int? sellerId)
    {
        using var connection = _context.CreateConnection();
        var stats = new DashboardStats();

        // 1. Base Filters
        string whereClause = sellerId.HasValue ? "WHERE seller_id = @SellerId" : "";
        string productWhere = sellerId.HasValue ? "WHERE seller_id = @SellerId" : "";

        // 2. Metrics
        // We map to a class (DashboardMetrics) to avoid case-sensitivity issues with 'dynamic'
        var metricsSql = $@"
            SELECT 
                COALESCE(SUM(total_amount), 0) as Revenue,
                COUNT(*) as Orders
            FROM orders {whereClause};
            
            SELECT COUNT(*) FROM products {productWhere};
        ";

        using (var multi = await connection.QueryMultipleAsync(metricsSql, new { SellerId = sellerId }))
        {
            // FIX: Read into the helper class instead of dynamic
            var metrics = await multi.ReadFirstAsync<DashboardMetrics>();
            stats.TotalRevenue = metrics.Revenue;
            stats.TotalOrders = metrics.Orders;

            stats.TotalProducts = await multi.ReadFirstAsync<int>();
        }

        // 3. Top Products
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

        // 4. Recent Orders
        var recentSql = $@"
            SELECT 
                id, 
                user_id::text as Customer, 
                product_name as Product, 
                COALESCE(total_amount, 0) as Amount, 
                status
            FROM orders
            {whereClause}
            ORDER BY created_at DESC
            LIMIT 5";
        stats.RecentOrders = (await connection.QueryAsync<RecentOrderDto>(recentSql, new { SellerId = sellerId })).ToList();

        return stats;
    }
}