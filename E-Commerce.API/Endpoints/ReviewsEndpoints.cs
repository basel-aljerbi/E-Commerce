using System.Security.Claims;
using E_Commerce.API.Data;
using E_Commerce.API.Dtos;
using E_Commerce.API.Infrastructure;
using E_Commerce.API.Models;
using E_Commerce.API.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.API.Endpoints;

public static class ReviewsEndpoints
{
    public static void MapReviewsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/products/{productId}/reviews");

        group.MapGet("/", async (
            int productId,
            ECommerceContext dbContext,
            CancellationToken ct) =>
        {
            var reviews = await dbContext.Reviews
                .Where(r => r.ProductId == productId)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewDto(
                    r.Id,
                    r.ProductId,
                    r.User.FullName ?? r.User.Email,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt
                ))
                .AsNoTracking()
                .ToListAsync(ct);

            var avgRating = reviews.Count != 0 ? reviews.Average(r => r.Rating) : 0;

            return ApiResult.Success(new
            {
                Reviews = reviews,
                AverageRating = Math.Round(avgRating, 1),
                TotalReviews = reviews.Count
            });
        });

        group.MapPost("/", [Authorize] async (
            int productId,
            CreateReviewDto dto,
            [FromServices] IValidator<CreateReviewDto> validator,
            ECommerceContext dbContext,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var error = await validator.ValidateAndReturnAsync(dto, ct);
            if (error is not null) return error;

            var userId = httpContext.User.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var productExists = await dbContext.Products
                .AnyAsync(p => p.Id == productId, ct);

            if (!productExists)
                return ApiResult.NotFound("Product not found");

            // Use AddIfNotExists pattern with unique constraint protection
            var existingReview = await dbContext.Reviews
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId, ct);

            if (existingReview is not null)
                return ApiResult.BadRequest("You have already reviewed this product");

            var review = new Review
            {
                ProductId = productId,
                UserId = userId.Value,
                Rating = dto.Rating,
                Comment = dto.Comment
            };

            dbContext.Reviews.Add(review);

            try
            {
                await dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException) when (dbContext.Reviews.Any(r =>
                r.ProductId == productId && r.UserId == userId))
            {
                return ApiResult.BadRequest("You have already reviewed this product");
            }

            return ApiResult.Created($"/products/{productId}/reviews/{review.Id}", review.Id);
        });

        group.MapDelete("/{reviewId}", [Authorize] async (
            int productId,
            int reviewId,
            ECommerceContext dbContext,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var userId = httpContext.User.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var review = await dbContext.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.ProductId == productId, ct);

            if (review is null)
                return ApiResult.NotFound("Review not found");

            var isAdmin = httpContext.User.IsInRole("Admin");
            if (review.UserId != userId && !isAdmin)
                return Results.Forbid();

            dbContext.Reviews.Remove(review);
            await dbContext.SaveChangesAsync(ct);

            return Results.NoContent();
        });
    }
}
