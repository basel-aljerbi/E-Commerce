using E_Commerce.API.Options;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace E_Commerce.API.Services;

public class EmailService
{
    private readonly SendGridOptions _sendGridOptions;
    private readonly string _frontendUrl;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<SendGridOptions> sendGridOptions,
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _sendGridOptions = sendGridOptions.Value;
        _frontendUrl = configuration["FrontendUrl"] ?? "http://localhost:3000";
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
    {
        try
        {
            var client = new SendGridClient(_sendGridOptions.ApiKey);
            var from = new EmailAddress(_sendGridOptions.FromEmail, _sendGridOptions.FromName);
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlContent);
            var response = await client.SendEmailAsync(msg);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Body.ReadAsStringAsync();
                _logger.LogWarning("Failed to send email to {Email}. Status: {Status}. Body: {Body}",
                    toEmail, response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw;
        }
    }

    public async Task SendVerificationEmailAsync(string toEmail, string code)
    {
        var html = $"""
            <h1>Welcome!</h1>
            <p>Your verification code is: <strong>{code}</strong></p>
            <p>This code will expire in 15 minutes.</p>
            <p>If you did not create an account, please ignore this email.</p>
            """;

        await SendEmailAsync(toEmail, "Verify Your Email", html);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string token)
    {
        var resetLink = $"{_frontendUrl}/reset-password?token={token}";
        var html = $"""
            <h1>Password Reset</h1>
            <p>Click the link below to reset your password:</p>
            <a href='{resetLink}' style='padding:10px 20px;background:#007bff;color:white;text-decoration:none;border-radius:5px;'>Reset Password</a>
            <p>This link will expire in 1 hour.</p>
            <p>If you did not request a password reset, please ignore this email.</p>
            """;

        await SendEmailAsync(toEmail, "Reset Your Password", html);
    }

    public async Task SendOrderConfirmationAsync(string toEmail, int orderId, decimal total)
    {
        var html = $"""
            <h1>Order Confirmation</h1>
            <p>Your order <strong>#{orderId}</strong> has been placed successfully!</p>
            <p>Total: <strong>${total:F2}</strong></p>
            <p>Thank you for shopping with us!</p>
            """;

        await SendEmailAsync(toEmail, $"Order #{orderId} Confirmed", html);
    }
}
