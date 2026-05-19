using System.Security.Claims;
using E_Commerce.API.Data;
using E_Commerce.API.Dtos;
using E_Commerce.API.Extensions;
using E_Commerce.API.Infrastructure;
using E_Commerce.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.API.Endpoints;

public static class WishlistEndpoints
{
    public static void MapWishlistEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/wishlist").RequireAuthorization();

        group.MapGet("/", async (
            ECommerceContext dbContext,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var userId = httpContext.User.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var items = await dbContext.WishlistItems
                .Where(w => w.UserId == userId)
                .Include(w => w.Product)
                .Select(w => new WishlistItemDto(
                    w.Id,
                    w.ProductId,
                    w.Product.Name,
                    w.Product.Price,
                    w.Product.ImageFileName != null ? "/images/" + w.Product.ImageFileName : null,
                    w.AddedAt
                ))
                .AsNoTracking()
                .ToListAsync(ct);

            return ApiResult.Success(items);
        });

        group.MapPost("/{productId}", async (
            int productId,
            ECommerceContext dbContext,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var userId = httpContext.User.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var productExists = await dbContext.Products
                .AnyAsync(p => p.Id == productId, ct);

            if (!productExists)
                return ApiResult.NotFound("Product not found");

            var existing = await dbContext.WishlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, ct);

            if (existing is not null)
                return ApiResult.BadRequest("Product already in wishlist");

            var item = new WishlistItem
            {
                UserId = userId.Value,
                ProductId = productId
            };

            dbContext.WishlistItems.Add(item);
            await dbContext.SaveChangesAsync(ct);

            return ApiResult.Created($"/wishlist/{item.Id}", item.Id);
        });

        group.MapDelete("/{productId}", async (
            int productId,
            ECommerceContext dbContext,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var userId = httpContext.User.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var item = await dbContext.WishlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, ct);

            if (item is null)
                return ApiResult.NotFound("Wishlist item not found");

            dbContext.WishlistItems.Remove(item);
            await dbContext.SaveChangesAsync(ct);

            return Results.NoContent();
        });
    }
}
