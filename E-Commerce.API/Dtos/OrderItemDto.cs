namespace E_Commerce.API.Dtos;

public record OrderItemDto(
    int Id,
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice
);
