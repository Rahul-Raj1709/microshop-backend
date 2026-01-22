namespace ProductAPI.Models;

public class WishlistItem : Product
{
    public DateTime AddedOn { get; set; }
}

public class WishlistRequest
{
    public int ProductId { get; set; }
}