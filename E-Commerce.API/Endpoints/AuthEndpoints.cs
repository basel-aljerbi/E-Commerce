using System.Security.Claims;
using E_Commerce.API.Data;
using E_Commerce.API.Dtos;
using E_Commerce.API.Infrastructure;
using E_Commerce.API.Models;
using E_Commerce.API.Services;
using E_Commerce.API.Extensions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.API.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/register", async (
            RegisterDto dto,
            IValidator<RegisterDto> validator,
            ECommerceContext dbContext,
            IBackgroundTaskQueue taskQueue,
            IServiceScopeFactory scopeFactory,
            IWebHostEnvironment env,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            var error = await validator.ValidateAndReturnAsync(dto, ct);
            if (error is not null) return error;

            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

            var emailExists = await dbContext.Users
                .AnyAsync(u => u.Email == normalizedEmail, ct);

            if (emailExists)
                return ApiResult.BadRequest("Email is already registered");

            var verificationCode = JwtService.GenerateCryptoToken();

            var isDev = env.IsDevelopment();

            var user = new User
            {
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12),
                FullName = dto.FullName.Trim(),
                Role = "User",
                IsEmailVerified = isDev,
                EmailVerificationCodeHash = isDev ? null : JwtService.ComputeTokenHash(verificationCode),
                EmailVerificationCodeExpires = isDev ? null : DateTime.UtcNow.AddMinutes(15)
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(ct);

            if (isDev)
            {
                logger.LogInformation(
                    "DEV: User {Email} auto-verified. Verification code would be: {Code}",
                    normalizedEmail, verificationCode);
                return ApiResult.Success(message: "Registration successful. (Dev mode: auto-verified)");
            }

            // Queue verification email as background job
            var email = user.Email;
            var code = verificationCode;
            await BackgroundJobs.SendEmail(
                taskQueue, scopeFactory,
                email,
                "Verify Your Email",
                $"""
                <h1>Welcome!</h1>
                <p>Your verification code is: <strong>{code}</strong></p>
                <p>This code will expire in 15 minutes.</p>
                <p>If you did not create an account, please ignore this email.</p>
                """);

            return ApiResult.Success(message: "Registration successful. Please check your email for verification code.");
        });

        group.MapPost("/verify-email", async (
            VerifyEmailDto dto,
            ECommerceContext dbContext,
            CancellationToken ct) =>
        {
            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

            if (user is null)
                return ApiResult.BadRequest("Invalid verification request");

            if (user.IsEmailVerified)
                return ApiResult.BadRequest("Email already verified");

            if (user.EmailVerificationCodeExpires is null ||
                user.EmailVerificationCodeExpires < DateTime.UtcNow)
            {
                return ApiResult.BadRequest("Verification code has expired");
            }

            var codeHash = JwtService.ComputeTokenHash(dto.Code);
            if (user.EmailVerificationCodeHash != codeHash)
                return ApiResult.BadRequest("Invalid verification code");

            user.IsEmailVerified = true;
            user.EmailVerificationCodeHash = null;
            user.EmailVerificationCodeExpires = null;

            await dbContext.SaveChangesAsync(ct);

            return ApiResult.Success(message: "Email verified successfully!");
        });

        group.MapPost("/login", async (
            LoginDto dto,
            IValidator<LoginDto> validator,
            ECommerceContext dbContext,
            JwtService jwtService,
            AuditService auditService,
            CancellationToken ct) =>
        {
            var error = await validator.ValidateAndReturnAsync(dto, ct);
            if (error is not null) return error;

            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

            if (user is null)
                return ApiResult.BadRequest("Invalid email or password");

            if (user.IsLockedOut)
                return ApiResult.BadRequest("Account is temporarily locked. Please try again later.");

            var validPassword = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

            if (!validPassword)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                }
                await dbContext.SaveChangesAsync(ct);
                return ApiResult.BadRequest("Invalid email or password");
            }

            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;

            if (!user.IsEmailVerified)
                return ApiResult.BadRequest("Please verify your email before logging in");

            var (token, expiresAt, jti) = jwtService.GenerateAccessToken(user);

            var refreshTokenValue = JwtService.GenerateRefreshToken();
            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = JwtService.ComputeTokenHash(refreshTokenValue),
                JwtId = jti,
                FamilyId = Guid.NewGuid(),
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            dbContext.RefreshTokens.Add(refreshToken);
            await dbContext.SaveChangesAsync(ct);

            await auditService.LogAsync(
                "Login", "User", user.Id,
                userId: user.Id, userEmail: user.Email,
                cancellationToken: ct);

            return ApiResult.Success(new AuthResponseDto(
                token, refreshTokenValue, expiresAt, user.Email, user.Role));
        });

        group.MapPost("/refresh", async (
            RefreshTokenRequestDto dto,
            ECommerceContext dbContext,
            JwtService jwtService,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(dto.RefreshToken))
                return ApiResult.BadRequest("Refresh token is required");

            var tokenHash = JwtService.ComputeTokenHash(dto.RefreshToken);

            var storedToken = await dbContext.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

            if (storedToken is null)
                return ApiResult.BadRequest("Invalid refresh token");

            if (storedToken.ExpiresAt < DateTime.UtcNow)
                return ApiResult.BadRequest("Refresh token has expired");

            // REUSE DETECTION: If token is already revoked, this is a token reuse attack
            if (storedToken.IsRevoked)
            {
                // Revoke entire token family (compromise detected)
                var familyTokens = await dbContext.RefreshTokens
                    .Where(rt => rt.FamilyId == storedToken.FamilyId && !rt.IsRevoked)
                    .ToListAsync(ct);
                foreach (var t in familyTokens)
                {
                    t.IsRevoked = true;
                    t.RevokedAt = DateTime.UtcNow;
                }
                await dbContext.SaveChangesAsync(ct);

                return ApiResult.BadRequest("Refresh token has been revoked due to suspected token reuse");
            }

            // Rotate: revoke current token
            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTime.UtcNow;

            // Issue new token in same family
            var newTokenValue = JwtService.GenerateRefreshToken();
            var newJti = Guid.NewGuid().ToString();

            var (newAccessToken, newExpiresAt, _) = jwtService.GenerateAccessToken(storedToken.User);

            var newRefreshToken = new RefreshToken
            {
                UserId = storedToken.UserId,
                TokenHash = JwtService.ComputeTokenHash(newTokenValue),
                JwtId = newJti,
                FamilyId = storedToken.FamilyId,
                PreviousTokenId = storedToken.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            dbContext.RefreshTokens.Add(newRefreshToken);
            await dbContext.SaveChangesAsync(ct);

            return ApiResult.Success(new AuthResponseDto(
                newAccessToken, newTokenValue, newExpiresAt,
                storedToken.User.Email, storedToken.User.Role));
        });

        group.MapPost("/logout", async (
            ClaimsPrincipal user,
            ECommerceContext dbContext,
            CancellationToken ct) =>
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim is null) return ApiResult.BadRequest("Invalid token");

            var userId = int.Parse(userIdClaim.Value);

            var activeTokens = await dbContext.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync(ct);
            foreach (var t in activeTokens)
            {
                t.IsRevoked = true;
                t.RevokedAt = DateTime.UtcNow;
            }
            await dbContext.SaveChangesAsync(ct);

            return ApiResult.Success(message: "Logged out successfully");
        });

        group.MapPost("/forgot-password", async (
            ForgotPasswordDto dto,
            ECommerceContext dbContext,
            IBackgroundTaskQueue taskQueue,
            IServiceScopeFactory scopeFactory,
            CancellationToken ct) =>
        {
            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

            if (user is null)
                return ApiResult.Success(message: "If the email exists, a reset link has been sent.");

            var resetToken = JwtService.GenerateCryptoToken();

            user.PasswordResetTokenHash = JwtService.ComputeTokenHash(resetToken);
            user.PasswordResetTokenExpires = DateTime.UtcNow.AddHours(1);

            await dbContext.SaveChangesAsync(ct);

            var email = user.Email;
            var token = resetToken;
            var frontendUrl = app.Configuration["FrontendUrl"] ?? "http://localhost:3000";
            var resetLink = $"{frontendUrl}/reset-password?token={token}";

            await BackgroundJobs.SendEmail(
                taskQueue, scopeFactory,
                email,
                "Reset Your Password",
                $"""
                <h1>Password Reset</h1>
                <p>Click the link below to reset your password:</p>
                <a href='{resetLink}' style='padding:10px 20px;background:#007bff;color:white;text-decoration:none;border-radius:5px;'>Reset Password</a>
                <p>This link will expire in 1 hour.</p>
                <p>If you did not request a password reset, please ignore this email.</p>
                """);

            return ApiResult.Success(message: "If the email exists, a reset link has been sent.");
        });

        group.MapPost("/reset-password", async (
            ResetPasswordDto dto,
            IValidator<ResetPasswordDto> validator,
            ECommerceContext dbContext,
            CancellationToken ct) =>
        {
            var error = await validator.ValidateAndReturnAsync(dto, ct);
            if (error is not null) return error;

            var tokenHash = JwtService.ComputeTokenHash(dto.Token);

            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.PasswordResetTokenHash == tokenHash, ct);

            if (user is null ||
                user.PasswordResetTokenExpires is null ||
                user.PasswordResetTokenExpires < DateTime.UtcNow)
            {
                return ApiResult.BadRequest("Invalid or expired reset token");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, workFactor: 12);
            user.PasswordResetTokenHash = null;
            user.PasswordResetTokenExpires = null;
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;

            // Revoke all refresh tokens for security
            var tokensToRevoke = await dbContext.RefreshTokens
                .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
                .ToListAsync(ct);
            foreach (var t in tokensToRevoke)
            {
                t.IsRevoked = true;
                t.RevokedAt = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync(ct);

            return ApiResult.Success(message: "Password reset successful!");
        });

        return group;
    }
}
