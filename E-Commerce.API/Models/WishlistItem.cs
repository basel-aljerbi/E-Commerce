namespace E_Commerce.API.Models;

public class WishlistItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = default!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = default!;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
