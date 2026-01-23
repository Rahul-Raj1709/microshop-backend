namespace AuthAPI.Models;

public class Store
{
    public int UserId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string BannerUrl { get; set; } = string.Empty;
    public string SupportEmail { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class StoreRequest
{
    public string StoreName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string BannerUrl { get; set; } = string.Empty;
    public string SupportEmail { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;
}