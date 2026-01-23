using System.ComponentModel.DataAnnotations;

namespace ProductAPI.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // New
    public string Description { get; set; } = string.Empty; // New
    public decimal Price { get; set; }
    public decimal? SalePrice { get; set; }
    public int Stock { get; set; }
    public int seller_id { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
// Helper class for pagination response
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}