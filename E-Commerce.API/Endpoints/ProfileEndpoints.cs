using System.Security.Claims;
using E_Commerce.API.Data;
using E_Commerce.API.Dtos;
using E_Commerce.API.Extensions;
using E_Commerce.API.Infrastructure;
using E_Commerce.API.Models;
using E_Commerce.API.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.API.Endpoints;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/profile").RequireAuthorization();

        group.MapGet("/", async (
            ClaimsPrincipal user,
            ECommerceContext db,
            CancellationToken ct) =>
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var profile = await db.Users
                .Where(u => u.Id == userId)
                .Select(u => new ProfileDto(
                    u.Id,
                    u.Email,
                    u.FullName ?? "",
                    u.PhoneNumber,
                    u.AvatarFileName != null ? "/images/" + u.AvatarFileName : null,
                    u.AddressLine1,
                    u.AddressLine2,
                    u.City,
                    u.State,
                    u.PostalCode,
                    u.Country,
                    u.Role,
                    u.IsEmailVerified,
                    u.CreatedAt,
                    u.UpdatedAt
                ))
                .FirstOrDefaultAsync(ct);

            if (profile is null)
                return ApiResult.NotFound("Profile not found");

            return ApiResult.Success(profile);
        });

        group.MapPut("/", async (
            UpdateProfileDto dto,
            IValidator<UpdateProfileDto> validator,
            ClaimsPrincipal user,
            ECommerceContext db,
            AuditService audit,
            CancellationToken ct) =>
        {
            var error = await validator.ValidateAndReturnAsync(dto, ct);
            if (error is not null) return error;

            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var profile = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (profile is null)
                return ApiResult.NotFound("Profile not found");

            var oldValues = new Dictionary<string, object?>
            {
                ["FullName"] = profile.FullName,
                ["PhoneNumber"] = profile.PhoneNumber,
                ["AddressLine1"] = profile.AddressLine1,
                ["AddressLine2"] = profile.AddressLine2,
                ["City"] = profile.City,
                ["State"] = profile.State,
                ["PostalCode"] = profile.PostalCode,
                ["Country"] = profile.Country
            };

            profile.FullName = dto.FullName.Trim();
            profile.PhoneNumber = dto.PhoneNumber?.Trim();
            profile.AddressLine1 = dto.AddressLine1?.Trim();
            profile.AddressLine2 = dto.AddressLine2?.Trim();
            profile.City = dto.City?.Trim();
            profile.State = dto.State?.Trim();
            profile.PostalCode = dto.PostalCode?.Trim();
            profile.Country = dto.Country?.Trim();
            profile.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            var newValues = new Dictionary<string, object?>
            {
                ["FullName"] = profile.FullName,
                ["PhoneNumber"] = profile.PhoneNumber,
                ["AddressLine1"] = profile.AddressLine1,
                ["AddressLine2"] = profile.AddressLine2,
                ["City"] = profile.City,
                ["State"] = profile.State,
                ["PostalCode"] = profile.PostalCode,
                ["Country"] = profile.Country
            };

            await audit.LogAsync("UpdateProfile", "User", userId, oldValues, newValues, userId, cancellationToken: ct);

            return ApiResult.Success(new ProfileDto(
                profile.Id,
                profile.Email,
                profile.FullName ?? "",
                profile.PhoneNumber,
                profile.AvatarFileName != null ? "/images/" + profile.AvatarFileName : null,
                profile.AddressLine1,
                profile.AddressLine2,
                profile.City,
                profile.State,
                profile.PostalCode,
                profile.Country,
                profile.Role,
                profile.IsEmailVerified,
                profile.CreatedAt,
                profile.UpdatedAt
            ), "Profile updated successfully");
        });

        group.MapPut("/password", async (
            ChangePasswordDto dto,
            IValidator<ChangePasswordDto> validator,
            ClaimsPrincipal user,
            ECommerceContext db,
            AuditService audit,
            CancellationToken ct) =>
        {
            var error = await validator.ValidateAndReturnAsync(dto, ct);
            if (error is not null) return error;

            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var profile = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (profile is null)
                return ApiResult.NotFound("Profile not found");

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, profile.PasswordHash))
                return ApiResult.BadRequest("Current password is incorrect");

            profile.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, workFactor: 12);
            profile.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            await audit.LogAsync("ChangePassword", "User", userId, null, null, userId, cancellationToken: ct);

            return ApiResult.Success(message: "Password changed successfully");
        });

        group.MapPost("/avatar", async (
            IFormFile file,
            ClaimsPrincipal user,
            ECommerceContext db,
            FileUploadService fileUpload,
            AuditService audit,
            CancellationToken ct) =>
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var profile = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (profile is null)
                return ApiResult.NotFound("Profile not found");

            var oldFileName = profile.AvatarFileName;

            var savedFileName = await fileUpload.SaveFileAsync(file, ct);

            if (oldFileName is not null)
            {
                fileUpload.DeleteFile(oldFileName);
            }

            profile.AvatarFileName = savedFileName;
            profile.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            await audit.LogAsync("UploadAvatar", "User", userId,
                oldFileName is not null ? new Dictionary<string, object?> { ["AvatarFileName"] = oldFileName } : null,
                new Dictionary<string, object?> { ["AvatarFileName"] = savedFileName },
                userId, cancellationToken: ct);

            return ApiResult.Success(new { avatarUrl = "/images/" + savedFileName }, "Avatar uploaded successfully");
        }).DisableAntiforgery();
    }
}
