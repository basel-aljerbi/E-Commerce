namespace E_Commerce.API.Dtos;

public record AuditLogDto(
    int Id,
    string Action,
    string EntityType,
    int? EntityId,
    string? UserEmail,
    string? OldValues,
    string? NewValues,
    DateTime Timestamp
);
