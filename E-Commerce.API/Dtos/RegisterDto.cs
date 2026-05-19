namespace E_Commerce.API.Dtos;

public record RegisterDto(
    string Email,
    string Password,
    string FullName
);
