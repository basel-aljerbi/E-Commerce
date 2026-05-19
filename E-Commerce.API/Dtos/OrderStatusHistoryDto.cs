using E_Commerce.API.Models.Enums;

namespace E_Commerce.API.Dtos;

public record OrderStatusHistoryDto(
    int Id,
    OrderStatus FromStatus,
    OrderStatus ToStatus,
    string Reason,
    DateTime ChangedAt
);
