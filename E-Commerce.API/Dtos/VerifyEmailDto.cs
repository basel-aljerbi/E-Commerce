namespace E_Commerce.API.Dtos;

public record VerifyEmailDto(
    string Email,
    string Code
);
