namespace E_Commerce.API.Dtos;

public record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    string? ImageUrl,
    string CategoryName
);
