using System.Security.Claims;
using E_Commerce.API.Data;
using E_Commerce.API.Dtos;
using E_Commerce.API.Extensions;
using E_Commerce.API.Infrastructure;
using E_Commerce.API.Models;
using E_Commerce.API.Models.Enums;
using E_Commerce.API.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.API.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/admin")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapGet("/dashboard", async (
            ECommerceContext db,
            CancellationToken ct) =>
        {
            var totalProducts = await db.Products.CountAsync(ct);
            var totalOrders = await db.Orders.CountAsync(ct);
            var totalUsers = await db.Users.CountAsync(ct);
            var totalRevenue = await db.Orders
                .Where(o => o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Paid)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var revenueByMonthRaw = await db.Orders
                .Where(o => o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Paid)
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync(ct);

            var revenueByMonth = revenueByMonthRaw
                .Select(x => new
                {
                    Month = $"{x.Year}-{x.Month:D2}",
                    x.Revenue
                })
                .ToList();

            var recentOrders = await db.Orders
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .Select(o => new OrderDto(
                    o.Id,
                    o.OrderDate,
                    o.TotalAmount,
                    o.Status,
                    o.OrderItems.Select(oi => new OrderItemDto(
                        oi.Id, oi.ProductId, oi.ProductName, oi.UnitPrice, oi.Quantity, oi.TotalPrice
                    )).ToList(),
                    null
                ))
                .ToListAsync(ct);

            return ApiResult.Success(new
            {
                TotalProducts = totalProducts,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                TotalUsers = totalUsers,
                RecentOrders = recentOrders,
                RevenueByMonth = revenueByMonth
            });
        });

        group.MapGet("/products", async (
            ECommerceContext db,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            CancellationToken ct = default) =>
        {
            var query = db.Products.IgnoreQueryFilters().Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Name.Contains(search) || (p.Description != null && p.Description.Contains(search)));

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(p => p.CreatedAt)
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

        group.MapPost("/products", async (
            [FromForm] CreateProductDto dto,
            [FromServices] IValidator<CreateProductDto> validator,
            [FromServices] FileUploadService fileUpload,
            [FromServices] AuditService audit,
            ClaimsPrincipal user,
            ECommerceContext db,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var error = await validator.ValidateAndReturnAsync(dto, ct);
            if (error is not null) return error;

            var categoryExists = await db.Categories.AnyAsync(c => c.Id == dto.CategoryId, ct);
            if (!categoryExists)
                return ApiResult.BadRequest("Category not found");

            string? imageFileName = null;
            if (dto.Image is not null && dto.Image.Length > 0)
            {
                try
                {
                    imageFileName = await fileUpload.SaveFileAsync(dto.Image, ct);
                }
                catch (InvalidOperationException ex)
                {
                    return ApiResult.BadRequest(ex.Message);
                }
            }

            var product = new Product
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                CategoryId = dto.CategoryId,
                ImageFileName = imageFileName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Products.Add(product);
            await db.SaveChangesAsync(ct);

            await audit.LogAsync("Created", "Product", product.Id,
                userId: user.GetUserId(),
                userEmail: httpContext.User.FindFirst(ClaimTypes.Email)?.Value,
                cancellationToken: ct);

            return ApiResult.Created($"/products/{product.Id}", new { id = product.Id });
        }).DisableAntiforgery();

        group.MapPut("/products/{id}", async (
            int id,
            [FromForm] CreateProductDto dto,
            [FromServices] IValidator<CreateProductDto> validator,
            [FromServices] FileUploadService fileUpload,
            [FromServices] AuditService audit,
            ClaimsPrincipal user,
            ECommerceContext db,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var product = await db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id, ct);
            if (product is null)
                return ApiResult.NotFound("Product not found");

            var error = await validator.ValidateAndReturnAsync(dto, ct);
            if (error is not null) return error;

            var categoryExists = await db.Categories.AnyAsync(c => c.Id == dto.CategoryId, ct);
            if (!categoryExists)
                return ApiResult.BadRequest("Category not found");

            var oldValues = new { product.Name, product.Price, product.StockQuantity, product.CategoryId };

            product.Name = dto.Name.Trim();
            product.Description = dto.Description?.Trim();
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;
            product.CategoryId = dto.CategoryId;
            product.UpdatedAt = DateTime.UtcNow;

            if (dto.Image is not null && dto.Image.Length > 0)
            {
                try
                {
                    var imageFileName = await fileUpload.SaveFileAsync(dto.Image, ct);
                    product.ImageFileName = imageFileName;
                }
                catch (InvalidOperationException ex)
                {
                    return ApiResult.BadRequest(ex.Message);
                }
            }

            await db.SaveChangesAsync(ct);

            await audit.LogAsync("Updated", "Product", product.Id,
                oldValues: oldValues,
                newValues: new { dto.Name, dto.Price, dto.StockQuantity, dto.CategoryId },
                userId: user.GetUserId(),
                userEmail: httpContext.User.FindFirst(ClaimTypes.Email)?.Value,
                cancellationToken: ct);

            return ApiResult.Success(message: "Product updated");
        }).DisableAntiforgery();

        group.MapDelete("/products/{id}", async (
            int id,
            [FromServices] AuditService audit,
            ClaimsPrincipal user,
            ECommerceContext db,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var product = await db.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id, ct);
            if (product is null)
                return ApiResult.NotFound("Product not found");

            product.IsDeleted = true;
            await db.SaveChangesAsync(ct);

            await audit.LogAsync("Deleted", "Product", id,
                oldValues: new { product.Name, product.Price },
                userId: user.GetUserId(),
                userEmail: httpContext.User.FindFirst(ClaimTypes.Email)?.Value,
                cancellationToken: ct);

            return ApiResult.Success(message: "Product deleted");
        });

        group.MapGet("/categories", async (
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

        group.MapPost("/categories", async (
            [FromForm] CreateCategoryDto dto,
            [FromServices] FileUploadService fileUpload,
            [FromServices] AuditService audit,
            ClaimsPrincipal user,
            ECommerceContext db,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var name = dto.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return ApiResult.BadRequest("Category name is required");

            var exists = await db.Categories.AnyAsync(c => c.Name == name, ct);
            if (exists)
                return ApiResult.BadRequest("Category already exists");

            string? imageFileName = null;
            if (dto.Image is not null && dto.Image.Length > 0)
            {
                try
                {
                    imageFileName = await fileUpload.SaveFileAsync(dto.Image, ct);
                }
                catch (InvalidOperationException ex)
                {
                    return ApiResult.BadRequest(ex.Message);
                }
            }

            var category = new Category { Name = name, ImageFileName = imageFileName };
            db.Categories.Add(category);
            await db.SaveChangesAsync(ct);

            await audit.LogAsync("Created", "Category", category.Id,
                userId: user.GetUserId(),
                userEmail: httpContext.User.FindFirst(ClaimTypes.Email)?.Value,
                cancellationToken: ct);

            return ApiResult.Success(new
            {
                id = category.Id,
                name = category.Name,
                imageUrl = category.ImageFileName != null ? "/images/" + category.ImageFileName : null
            });
        }).DisableAntiforgery();

        group.MapPut("/categories/{id}", async (
            int id,
            [FromForm] CreateCategoryDto dto,
            [FromServices] FileUploadService fileUpload,
            [FromServices] AuditService audit,
            ClaimsPrincipal user,
            ECommerceContext db,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var name = dto.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return ApiResult.BadRequest("Category name is required");

            var category = await db.Categories.FindAsync([id], ct);
            if (category is null)
                return ApiResult.NotFound("Category not found");

            var exists = await db.Categories.AnyAsync(c => c.Name == name && c.Id != id, ct);
            if (exists)
                return ApiResult.BadRequest("Category name already taken");

            var oldName = category.Name;
            var oldImage = category.ImageFileName;
            category.Name = name;

            if (dto.Image is not null && dto.Image.Length > 0)
            {
                try
                {
                    var newFileName = await fileUpload.SaveFileAsync(dto.Image, ct);
                    fileUpload.DeleteFile(category.ImageFileName);
                    category.ImageFileName = newFileName;
                }
                catch (InvalidOperationException ex)
                {
                    return ApiResult.BadRequest(ex.Message);
                }
            }

            await db.SaveChangesAsync(ct);

            await audit.LogAsync("Updated", "Category", id,
                oldValues: new { Name = oldName, Image = oldImage },
                newValues: new { Name = name, Image = category.ImageFileName },
                userId: user.GetUserId(),
                userEmail: httpContext.User.FindFirst(ClaimTypes.Email)?.Value,
                cancellationToken: ct);

            return ApiResult.Success(message: "Category updated");
        }).DisableAntiforgery();

        group.MapDelete("/categories/{id}", async (
            int id,
            ECommerceContext db,
            [FromServices] AuditService audit,
            ClaimsPrincipal user,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var category = await db.Categories.FindAsync([id], ct);
            if (category is null)
                return ApiResult.NotFound("Category not found");

            var hasProducts = await db.Products.AnyAsync(p => p.CategoryId == id, ct);
            if (hasProducts)
                return ApiResult.BadRequest("Category has products — reassign or delete them first");

            db.Categories.Remove(category);
            await db.SaveChangesAsync(ct);

            await audit.LogAsync("Deleted", "Category", id,
                oldValues: new { category.Name },
                userId: user.GetUserId(),
                userEmail: httpContext.User.FindFirst(ClaimTypes.Email)?.Value,
                cancellationToken: ct);

            return ApiResult.Success(message: "Category deleted");
        });

        group.MapGet("/orders", async (
            ECommerceContext db,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            CancellationToken ct = default) =>
        {
            var query = db.Orders.Include(o => o.OrderItems).AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var parsedStatus))
                query = query.Where(o => o.Status == parsedStatus);

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderDto(
                    o.Id,
                    o.OrderDate,
                    o.TotalAmount,
                    o.Status,
                    o.OrderItems.Select(oi => new OrderItemDto(
                        oi.Id, oi.ProductId, oi.ProductName, oi.UnitPrice, oi.Quantity, oi.TotalPrice
                    )).ToList(),
                    null
                ))
                .ToListAsync(ct);

            return ApiResult.Success(new PagedResult<OrderDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        });

        group.MapPut("/orders/{id}/status", async (
            int id,
            ECommerceContext db,
            string newStatus,
            [FromServices] AuditService audit,
            ClaimsPrincipal user,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<OrderStatus>(newStatus, ignoreCase: true, out var parsedStatus))
                return ApiResult.BadRequest($"Invalid status: {newStatus}");

            var order = await db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id, ct);

            if (order is null)
                return ApiResult.NotFound("Order not found");

            if (order.Status == parsedStatus)
                return ApiResult.BadRequest("Order is already in this status");

            var previousStatus = order.Status;
            order.Status = parsedStatus;
            order.UpdatedAt = DateTime.UtcNow;

            db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = previousStatus,
                ToStatus = parsedStatus,
                Reason = $"Status changed by admin",
                ChangedByUserId = user.GetUserId(),
                ChangedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync(ct);

            await audit.LogAsync("Updated", "Order", id,
                oldValues: new { Status = previousStatus.ToString() },
                newValues: new { Status = parsedStatus.ToString() },
                userId: user.GetUserId(),
                userEmail: httpContext.User.FindFirst(ClaimTypes.Email)?.Value,
                cancellationToken: ct);

            return ApiResult.Success(message: $"Order status updated to {parsedStatus}");
        });

        group.MapGet("/users", async (
            ECommerceContext db,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            CancellationToken ct = default) =>
        {
            var query = db.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.Email.Contains(search) || (u.FullName != null && u.FullName.Contains(search)));

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FullName,
                    u.Role,
                    u.IsEmailVerified,
                    u.CreatedAt
                })
                .ToListAsync(ct);

            return ApiResult.Success(new PagedResult<object>
            {
                Items = items.Cast<object>().ToList(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        });

        group.MapPut("/users/{id}", async (
            int id,
            ECommerceContext db,
            [FromBody] UpdateUserDto dto,
            [FromServices] AuditService audit,
            ClaimsPrincipal user,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var currentUserId = user.GetUserId();
            if (id == currentUserId)
                return ApiResult.BadRequest("Cannot modify your own account here");

            var target = await db.Users.FindAsync([id], ct);
            if (target is null)
                return ApiResult.NotFound("User not found");

            var oldRole = target.Role;

            if (!string.IsNullOrWhiteSpace(dto.Role))
            {
                if (dto.Role != "User" && dto.Role != "Admin")
                    return ApiResult.BadRequest("Role must be 'User' or 'Admin'");
                target.Role = dto.Role;
            }

            await db.SaveChangesAsync(ct);

            await audit.LogAsync("Updated", "User", id,
                oldValues: new { Role = oldRole },
                newValues: new { Role = target.Role },
                userId: currentUserId,
                userEmail: httpContext.User.FindFirst(ClaimTypes.Email)?.Value,
                cancellationToken: ct);

            return ApiResult.Success(message: "User updated");
        });

        group.MapGet("/payments", async (
            ECommerceContext db,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default) =>
        {
            var query = db.Payments.Include(p => p.Order).AsQueryable();

            var totalCount = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.OrderId,
                    p.Amount,
                    p.Currency,
                    p.Status,
                    p.StripePaymentIntentId,
                    p.CreatedAt
                })
                .ToListAsync(ct);

            return ApiResult.Success(new PagedResult<object>
            {
                Items = items.Cast<object>().ToList(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        });
    }
}

public record UpdateUserDto
{
    public string? Role { get; init; }
}

public record CreateCategoryRequest
{
    public string Name { get; init; } = string.Empty;
}
