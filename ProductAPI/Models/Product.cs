using System.ComponentModel.DataAnnotations;

namespace ProductAPI.Models;

public class Product
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public decimal Price { get; set; }

    [Required]
    public int Stock { get; set; }

    // Matches the database column "seller_id" exactly
    public int seller_id { get; set; }
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