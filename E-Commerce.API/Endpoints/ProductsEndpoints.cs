using E_Commerce.API.Data;
using E_Commerce.API.Dtos;
using E_Commerce.API.Infrastructure;
using E_Commerce.API.Models;
using E_Commerce.API.Services;
using E_Commerce.API.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace E_Commerce.API.Endpoints;

public static class ProductsEndpoints
{
    public static void MapProductsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/products");

        group.MapGet("", async (
            int? categoryId,
            string? search,
            decimal? minPrice,
            decimal? maxPrice,
            string? sortBy,
            string? sortOrder,
            ECommerceContext dbContext,
            CancellationToken ct) =>
        {
            var pageNumber = 1;
            var pageSize = 10;
            var sortOrderValue = sortOrder ?? "asc";

            var productsQuery = dbContext.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .AsQueryable();

            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p =>
                    p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                productsQuery = productsQuery.Where(p =>
                    EF.Functions.Like(p.Name, $"%{s}%"));
            }

            if (minPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p =>
                    p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p =>
                    p.Price <= maxPrice.Value);
            }

            productsQuery = (sortBy?.ToLowerInvariant(), sortOrderValue.ToLowerInvariant()) switch
            {
                ("name", "desc") => productsQuery.OrderByDescending(p => p.Name),
                ("name", _) => productsQuery.OrderBy(p => p.Name),
                ("price", "desc") => productsQuery.OrderByDescending(p => p.Price),
                ("price", _) => productsQuery.OrderBy(p => p.Price),
                ("date", "desc") => productsQuery.OrderByDescending(p => p.CreatedAt),
                ("date", _) => productsQuery.OrderBy(p => p.CreatedAt),
                _ => productsQuery.OrderBy(p => p.Id)
            };

            var totalCount = await productsQuery.CountAsync(ct);

            var items = await productsQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto(
                    p.Id,
                    p.Name,
                    p.Description,
                    p.Price,
                    p.StockQuantity,
                    p.ImageFileName != null ? "/images/" + p.ImageFileName : null,
                    p.Category.Name
                ))
                .ToListAsync(ct);

            return ApiResult.Success(new PagedResult<ProductDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        });

        group.MapGet("/{id}", async (
            int id,
            ECommerceContext dbContext,
            CancellationToken ct) =>
        {
            var product = await dbContext.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id, ct);

            return product is null
                ? ApiResult.NotFound("Product not found")
                : ApiResult.Success(new ProductDto(
                    product.Id,
                    product.Name,
                    product.Description,
                    product.Price,
                    product.StockQuantity,
                    product.ImageFileName != null ? "/images/" + product.ImageFileName : null,
                    product.Category.Name));
        });

        group.MapPost("/", [Authorize(Roles = "Admin")] async (
            [FromForm] CreateProductDto newProduct,
            [FromServices] IValidator<CreateProductDto> validator,
            ECommerceContext dbContext,
            FileUploadService fileUploadService,
            AuditService auditService,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var error = await validator.ValidateAndReturnAsync(newProduct, ct);
            if (error is not null) return error;

            var categoryExists = await dbContext.Categories
                .AnyAsync(c => c.Id == newProduct.CategoryId, ct);

            if (!categoryExists)
                return ApiResult.BadRequest($"Category with id {newProduct.CategoryId} not found");

            string? imageFileName = null;
            if (newProduct.Image is not null)
            {
                try
                {
                    imageFileName = await fileUploadService.SaveFileAsync(newProduct.Image, ct);
                }
                catch (InvalidOperationException ex)
                {
                    return ApiResult.BadRequest(ex.Message);
                }
            }

            var product = new Product
            {
                Name = newProduct.Name,
                Description = newProduct.Description,
                Price = newProduct.Price,
                StockQuantity = newProduct.StockQuantity,
                ImageFileName = imageFileName,
                CategoryId = newProduct.CategoryId
            };

            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync(ct);

            var userId = int.Parse(
                httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userEmail = httpContext.User.FindFirst(ClaimTypes.Email)!.Value;

            await auditService.LogAsync(
                "Created", "Product", product.Id,
                userId: userId, userEmail: userEmail,
                cancellationToken: ct);

            return ApiResult.Created($"/products/{product.Id}", product.Id);
        }).DisableAntiforgery();

        group.MapPut("/{id}", [Authorize(Roles = "Admin")] async (
            int id,
            [FromForm] CreateProductDto updatedProduct,
            [FromServices] IValidator<CreateProductDto> validator,
            ECommerceContext dbContext,
            FileUploadService fileUploadService,
            AuditService auditService,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var error = await validator.ValidateAndReturnAsync(updatedProduct, ct);
            if (error is not null) return error;

            var existingProduct = await dbContext.Products
                .FindAsync([id], cancellationToken: ct);

            if (existingProduct is null)
                return ApiResult.NotFound("Product not found");

            var categoryExists = await dbContext.Categories
                .AnyAsync(c => c.Id == updatedProduct.CategoryId, ct);

            if (!categoryExists)
                return ApiResult.BadRequest($"Category with id {updatedProduct.CategoryId} not found");

            var oldValues = new
            {
                existingProduct.Name,
                existingProduct.Description,
                existingProduct.Price,
                existingProduct.StockQuantity
            };

            if (updatedProduct.Image is not null)
            {
                try
                {
                    var newFileName = await fileUploadService.SaveFileAsync(updatedProduct.Image, ct);
                    fileUploadService.DeleteFile(existingProduct.ImageFileName);
                    existingProduct.ImageFileName = newFileName;
                }
                catch (InvalidOperationException ex)
                {
                    return ApiResult.BadRequest(ex.Message);
                }
            }

            existingProduct.Name = updatedProduct.Name;
            existingProduct.Description = updatedProduct.Description;
            existingProduct.Price = updatedProduct.Price;
            existingProduct.StockQuantity = updatedProduct.StockQuantity;
            existingProduct.CategoryId = updatedProduct.CategoryId;
            existingProduct.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(ct);

            var userId = int.Parse(
                httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userEmail = httpContext.User.FindFirst(ClaimTypes.Email)!.Value;

            await auditService.LogAsync(
                "Updated", "Product", existingProduct.Id,
                oldValues, existingProduct, userId, userEmail,
                cancellationToken: ct);

            return Results.NoContent();
        }).DisableAntiforgery();

        group.MapDelete("/{id}", [Authorize(Roles = "Admin")] async (
            int id,
            ECommerceContext dbContext,
            AuditService auditService,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var product = await dbContext.Products
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == id, ct);

            if (product is null)
                return ApiResult.NotFound("Product not found");

            product.IsDeleted = true;
            await dbContext.SaveChangesAsync(ct);

            var userId = int.Parse(
                httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var userEmail = httpContext.User.FindFirst(ClaimTypes.Email)!.Value;

            await auditService.LogAsync(
                "Deleted", "Product", product.Id,
                userId: userId, userEmail: userEmail,
                cancellationToken: ct);

            return Results.NoContent();
        });
    }

    public static void MapPublicCategoriesEndpoint(this WebApplication app)
    {
        app.MapGet("/categories", async (
            ECommerceContext db,
            CancellationToken ct) =>
        {
            var categories = await db.Categories
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    ImageUrl = c.ImageFileName != null ? "/images/" + c.ImageFileName : null
                })
                .ToListAsync(ct);
            return ApiResult.Success(categories);
        });
    }
}
