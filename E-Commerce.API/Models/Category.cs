namespace E_Commerce.API.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageFileName { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
