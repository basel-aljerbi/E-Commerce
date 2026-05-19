namespace E_Commerce.API.Dtos;

public record ProfileDto(
    int Id,
    string Email,
    string FullName,
    string? PhoneNumber,
    string? AvatarUrl,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? PostalCode,
    string? Country,
    string Role,
    bool IsEmailVerified,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record UpdateProfileDto(
    string FullName,
    string? PhoneNumber,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? PostalCode,
    string? Country
);

public record ChangePasswordDto(
    string CurrentPassword,
    string NewPassword
);
