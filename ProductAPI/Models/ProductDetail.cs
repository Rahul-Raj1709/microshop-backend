namespace ProductAPI.Models;

public class ProductDetail : Product
{
    // Seller Info
    public string SellerName { get; set; } = string.Empty;
    public string SellerEmail { get; set; } = string.Empty;

    // Aggregated Rating Info
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }

    // List of individual reviews
    public List<ProductReview> Reviews { get; set; } = new();
}

public class ProductReview
{
    public string ReviewerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Feedback { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}