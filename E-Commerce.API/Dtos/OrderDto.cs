using E_Commerce.API.Models.Enums;

namespace E_Commerce.API.Dtos;

public record OrderDto(
    int Id,
    DateTime OrderDate,
    decimal TotalAmount,
    OrderStatus Status,
    List<OrderItemDto> Items,
    List<OrderStatusHistoryDto>? StatusHistory
);
