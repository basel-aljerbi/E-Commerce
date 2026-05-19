namespace E_Commerce.API.Models;

public class CartItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
