namespace E_Commerce.API.Dtos;

public record WishlistItemDto(
    int Id,
    int ProductId,
    string ProductName,
    decimal ProductPrice,
    string? ImageUrl,
    DateTime AddedAt
);
