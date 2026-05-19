namespace E_Commerce.API.Dtos;

public record AuthResponseDto(
    string Token,
    string RefreshToken,
    DateTime ExpiresAt,
    string Email,
    string Role
);
