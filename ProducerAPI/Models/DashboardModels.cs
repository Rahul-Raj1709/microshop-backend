namespace ProducerAPI.Models;

public class DashboardStats
{
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int TotalProducts { get; set; }
    public List<TopProductDto> TopProducts { get; set; }
    public List<RecentOrderDto> RecentOrders { get; set; }
}

public class TopProductDto
{
    public string Name { get; set; }
    public int Sales { get; set; }
    public decimal Revenue { get; set; }
}

public class RecentOrderDto
{
    public int Id { get; set; }
    public string Customer { get; set; }
    public string Product { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; }
}