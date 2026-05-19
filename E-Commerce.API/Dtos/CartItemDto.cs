namespace E_Commerce.API.Dtos;

public record CartItemDto(
    int Id,
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice
);
