namespace E_Commerce.API.Dtos;

public record ResetPasswordDto(
    string Token,
    string NewPassword
);
