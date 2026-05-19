namespace E_Commerce.API.Dtos;

public record PaymentResponseDto(
    string ClientSecret,
    string Status,
    string Message
);
