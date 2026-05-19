namespace E_Commerce.API.Dtos;

public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public IFormFile? Image { get; set; }
}
