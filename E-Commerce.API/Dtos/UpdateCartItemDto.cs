using System.ComponentModel.DataAnnotations;

namespace E_Commerce.API.Dtos;

public record UpdateCartItemDto(
    [Range(1, 1000)] int Quantity
);
