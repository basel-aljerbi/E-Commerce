using System.Security.Claims;
using E_Commerce.API.Data;
using E_Commerce.API.Dtos;
using E_Commerce.API.Extensions;
using E_Commerce.API.Infrastructure;
using E_Commerce.API.Models;
using E_Commerce.API.Models.Enums;
using E_Commerce.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.API.Endpoints;

public static class OrdersEndpoints
{
    public static void MapOrdersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/orders").RequireAuthorization();

        group.MapGet("/", async (
            ClaimsPrincipal user,
            ECommerceContext dbContext,
            CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var isAdmin = user.IsInRole("Admin");

            var query = dbContext.Orders
                .Include(o => o.OrderItems)
                .AsNoTracking();

            if (!isAdmin)
            {
                query = query.Where(o => o.UserId == userId);
            }

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new OrderDto(
                    o.Id,
                    o.OrderDate,
                    o.TotalAmount,
                    o.Status,
                    o.OrderItems.Select(oi => new OrderItemDto(
                        oi.Id,
                        oi.ProductId,
                        oi.ProductName,
                        oi.UnitPrice,
                        oi.Quantity,
                        oi.TotalPrice
                    )).ToList(),
                    null
                ))
                .ToListAsync(ct);

            return ApiResult.Success(orders);
        });

        group.MapGet("/{id}", async (
            int id,
            ClaimsPrincipal user,
            ECommerceContext dbContext,
            CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var isAdmin = user.IsInRole("Admin");

            var query = dbContext.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.StatusHistory.OrderBy(sh => sh.ChangedAt))
                .AsNoTracking();

            if (!isAdmin)
            {
                query = query.Where(o => o.UserId == userId);
            }

            var order = await query
                .FirstOrDefaultAsync(o => o.Id == id, ct);

            if (order is null)
                return ApiResult.NotFound("Order not found");

            var orderDto = new OrderDto(
                order.Id,
                order.OrderDate,
                order.TotalAmount,
                order.Status,
                order.OrderItems.Select(oi => new OrderItemDto(
                    oi.Id,
                    oi.ProductId,
                    oi.ProductName,
                    oi.UnitPrice,
                    oi.Quantity,
                    oi.TotalPrice
                )).ToList(),
                order.StatusHistory.Select(sh => new OrderStatusHistoryDto(
                    sh.Id,
                    sh.FromStatus,
                    sh.ToStatus,
                    sh.Reason,
                    sh.ChangedAt
                )).ToList()
            );

            return ApiResult.Success(orderDto);
        });

        group.MapPost("/checkout", async (
            ClaimsPrincipal user,
            ECommerceContext dbContext,
            IBackgroundTaskQueue taskQueue,
            IServiceScopeFactory scopeFactory,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var maxRetries = 3;
            for (var attempt = 1; attempt <= maxRetries; attempt++)
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

                try
                {
                    var cartItems = await dbContext.CartItems
                        .Include(c => c.Product)
                        .Where(c => c.UserId == userId)
                        .ToListAsync(ct);

                    if (cartItems.Count == 0)
                        return ApiResult.BadRequest("Cart is empty");

                    foreach (var item in cartItems)
                    {
                        if (item.Product is null || item.Product.StockQuantity < item.Quantity)
                        {
                            return ApiResult.BadRequest(
                                $"Not enough stock for '{item.Product?.Name ?? "Unknown"}'. Available: {item.Product?.StockQuantity ?? 0}");
                        }
                    }

                    var totalAmount = cartItems.Sum(c => c.UnitPrice * c.Quantity);

                    var order = new Order
                    {
                        OrderDate = DateTime.UtcNow,
                        Status = OrderStatus.Pending,
                        TotalAmount = totalAmount,
                        UserId = userId.Value
                    };

                    dbContext.Orders.Add(order);
                    await dbContext.SaveChangesAsync(ct);

                    foreach (var item in cartItems)
                    {
                        dbContext.OrderItems.Add(new OrderItem
                        {
                            OrderId = order.Id,
                            ProductId = item.ProductId,
                            ProductName = item.Product!.Name,
                            UnitPrice = item.UnitPrice,
                            Quantity = item.Quantity
                        });

                        item.Product.StockQuantity -= item.Quantity;
                    }

                    dbContext.OrderStatusHistories.Add(new OrderStatusHistory
                    {
                        OrderId = order.Id,
                        FromStatus = OrderStatus.Pending,
                        ToStatus = OrderStatus.Pending,
                        Reason = "Order created",
                        ChangedAt = DateTime.UtcNow
                    });

                    dbContext.CartItems.RemoveRange(cartItems);
                    await dbContext.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);

                    var userEmail = httpContext.User.FindFirst(ClaimTypes.Email)?.Value;
                    if (userEmail is not null)
                    {
                        var email = userEmail;
                        var orderId = order.Id;
                        var total = order.TotalAmount;
                        await BackgroundJobs.SendEmail(
                            taskQueue, scopeFactory,
                            email,
                            $"Order #{orderId} Confirmed",
                            $"""
                            <h1>Order Confirmation</h1>
                            <p>Your order <strong>#{orderId}</strong> has been placed successfully!</p>
                            <p>Total: <strong>${total:F2}</strong></p>
                            <p>Thank you for shopping with us!</p>
                            """);
                    }

                    return ApiResult.Created($"/orders/{order.Id}", new
                    {
                        Message = "Order placed successfully",
                        OrderId = order.Id,
                        TotalAmount = order.TotalAmount
                    });
                }
                catch (DbUpdateConcurrencyException) when (attempt < maxRetries)
                {
                    await transaction.RollbackAsync(ct);
                    await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), ct);
                    continue;
                }
                catch
                {
                    await transaction.RollbackAsync(ct);
                    throw;
                }
            }

            return ApiResult.BadRequest("Checkout failed due to concurrent activity. Please try again.");

        });

        group.MapPut("/{id}/cancel", async (
            int id,
            ClaimsPrincipal user,
            ECommerceContext dbContext,
            CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return Results.Unauthorized();

            var isAdmin = user.IsInRole("Admin");

            var query = dbContext.Orders
                .Include(o => o.OrderItems)
                .AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(o => o.UserId == userId);
            }

            var order = await query.FirstOrDefaultAsync(o => o.Id == id, ct);

            if (order is null)
                return ApiResult.NotFound("Order not found");

            if (order.Status == OrderStatus.Cancelled)
                return ApiResult.BadRequest("Order is already cancelled");

            if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Shipped)
                return ApiResult.BadRequest("Order cannot be cancelled at this stage");

            var previousStatus = order.Status;

            foreach (var item in order.OrderItems)
            {
                var product = await dbContext.Products.FindAsync([item.ProductId], cancellationToken: ct);
                if (product is not null)
                {
                    product.StockQuantity += item.Quantity;
                }
            }

            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;

            dbContext.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = previousStatus,
                ToStatus = OrderStatus.Cancelled,
                Reason = isAdmin ? "Cancelled by admin" : "Cancelled by user",
                ChangedByUserId = userId,
                ChangedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync(ct);

            return ApiResult.Success(message: "Order cancelled successfully");
        });

        // Admin-only endpoint to update order status
        group.MapPut("/{id}/status", [Authorize(Roles = "Admin")] async (
            int id,
            string newStatus,
            ClaimsPrincipal user,
            ECommerceContext dbContext,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<OrderStatus>(newStatus, ignoreCase: true, out var parsedStatus))
                return ApiResult.BadRequest($"Invalid status: {newStatus}");

            var order = await dbContext.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id, ct);

            if (order is null)
                return ApiResult.NotFound("Order not found");

            if (order.Status == parsedStatus)
                return ApiResult.BadRequest("Order is already in this status");

            var previousStatus = order.Status;
            order.Status = parsedStatus;
            order.UpdatedAt = DateTime.UtcNow;

            dbContext.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = previousStatus,
                ToStatus = parsedStatus,
                Reason = $"Status changed by admin",
                ChangedByUserId = user.GetUserId(),
                ChangedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync(ct);

            return ApiResult.Success(message: $"Order status updated to {parsedStatus}");
        });
    }
}
