using System.Security.Claims;
using E_Commerce.API.Data;
using E_Commerce.API.Dtos;
using E_Commerce.API.Infrastructure;
using E_Commerce.API.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.API.Endpoints;

public static class CartEndpoints
{
    public static void MapCartEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/cart").RequireAuthorization();

        group.MapGet("/", async (
            ClaimsPrincipal user,
            ECommerceContext dbContext,
            CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var cartItems = await dbContext.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .Select(c => new CartItemDto(
                    c.Id,
                    c.ProductId,
                    c.Product.Name,
                    c.UnitPrice,
                    c.Quantity,
                    c.UnitPrice * c.Quantity
                ))
                .AsNoTracking()
                .ToListAsync(ct);

            return ApiResult.Success(new
            {
                Items = cartItems,
                TotalAmount = cartItems.Sum(c => c.TotalPrice),
                ItemCount = cartItems.Sum(c => c.Quantity)
            });
        });

        group.MapPost("/", async (
            AddToCartDto dto,
            [FromServices] IValidator<AddToCartDto> validator,
            ClaimsPrincipal user,
            ECommerceContext dbContext,
            CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var error = await validator.ValidateAndReturnAsync(dto, ct);
            if (error is not null) return error;

            var product = await dbContext.Products.FindAsync([dto.ProductId], cancellationToken: ct);
            if (product is null)
                return ApiResult.NotFound($"Product {dto.ProductId} not found");

            if (product.StockQuantity < dto.Quantity)
                return ApiResult.BadRequest($"Not enough stock. Available: {product.StockQuantity}");

            var existingItem = await dbContext.CartItems
                .FirstOrDefaultAsync(c =>
                    c.ProductId == dto.ProductId &&
                    c.UserId == userId, ct);

            if (existingItem is not null)
            {
                existingItem.Quantity += dto.Quantity;
                if (existingItem.Quantity > product.StockQuantity)
                    return ApiResult.BadRequest($"Not enough stock. Available: {product.StockQuantity}");
            }
            else
            {
                dbContext.CartItems.Add(new Models.CartItem
                {
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity,
                    UnitPrice = product.Price,
                    UserId = userId.Value
                });
            }

            await dbContext.SaveChangesAsync(ct);
            return ApiResult.Success(message: "Item added to cart");
        });

        group.MapPut("/{id}", async (
            int id,
            UpdateCartItemDto dto,
            ClaimsPrincipal user,
            ECommerceContext dbContext,
            CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var cartItem = await dbContext.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);

            if (cartItem is null)
                return ApiResult.NotFound("Cart item not found");

            if (dto.Quantity < 1)
                return ApiResult.BadRequest("Quantity must be at least 1");

            if (cartItem.Product is not null && cartItem.Product.StockQuantity < dto.Quantity)
                return ApiResult.BadRequest($"Only {cartItem.Product.StockQuantity} items available");

            cartItem.Quantity = dto.Quantity;
            await dbContext.SaveChangesAsync(ct);

            return Results.NoContent();
        });

        group.MapDelete("/{id}", async (
            int id,
            ClaimsPrincipal user,
            ECommerceContext dbContext,
            CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var cartItem = await dbContext.CartItems
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);

            if (cartItem is null)
                return ApiResult.NotFound("Cart item not found");

            dbContext.CartItems.Remove(cartItem);
            await dbContext.SaveChangesAsync(ct);

            return Results.NoContent();
        });

        group.MapDelete("/", async (
            ClaimsPrincipal user,
            ECommerceContext dbContext,
            CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var itemsToDelete = await dbContext.CartItems
                .Where(c => c.UserId == userId)
                .ToListAsync(ct);
            dbContext.CartItems.RemoveRange(itemsToDelete);
            await dbContext.SaveChangesAsync(ct);

            return Results.NoContent();
        });
    }
}
