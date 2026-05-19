using E_Commerce.API.Data;
using E_Commerce.API.Dtos;
using E_Commerce.API.Infrastructure;
using E_Commerce.API.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.API.Endpoints;

public static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/admin/analytics")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapGet("/overview", async (
            ECommerceContext db,
            CancellationToken ct) =>
        {
            var totalUsers = await db.Users.CountAsync(ct);
            var totalOrders = await db.Orders.CountAsync(ct);
            var totalProducts = await db.Products.IgnoreQueryFilters().CountAsync(ct);
            var totalRevenue = await db.Orders
                .Where(o => o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Paid)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            var lowStockProducts = await db.Products.IgnoreQueryFilters()
                .CountAsync(p => p.StockQuantity > 0 && p.StockQuantity <= 5, ct);
            var pendingOrders = await db.Orders
                .CountAsync(o => o.Status == OrderStatus.Pending, ct);
            var newUsersToday = await db.Users
                .CountAsync(u => u.CreatedAt >= DateTime.UtcNow.Date, ct);
            var avgOrderValue = totalOrders > 0
                ? Math.Round(totalRevenue / totalOrders, 2)
                : 0;
            var totalReviews = await db.Reviews.IgnoreQueryFilters().CountAsync(ct);

            return ApiResult.Success(new AnalyticsOverviewDto(
                totalUsers, totalOrders, totalRevenue, totalProducts,
                lowStockProducts, pendingOrders, newUsersToday, avgOrderValue, totalReviews
            ));
        });

        group.MapGet("/revenue-trends", async (
            ECommerceContext db,
            string? period = "month",
            CancellationToken ct = default) =>
        {
            var query = db.Orders
                .Where(o => o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Paid);

            List<RevenueTrendDto> trends;
            if (period == "day")
            {
                var raw = await query
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount), Count = g.Count() })
                    .OrderByDescending(x => x.Date)
                    .Take(30)
                    .ToListAsync(ct);

                trends = raw.OrderBy(x => x.Date)
                    .Select(x => new RevenueTrendDto(x.Date.ToString("yyyy-MM-dd"), x.Revenue, x.Count))
                    .ToList();
            }
            else if (period == "week")
            {
                var all = await query
                    .Select(o => new { o.OrderDate, o.TotalAmount })
                    .ToListAsync(ct);

                trends = all
                    .GroupBy(o => GetWeekStart(o.OrderDate))
                    .Select(g => new RevenueTrendDto(
                        g.Key.ToString("yyyy-MM-dd"),
                        g.Sum(o => o.TotalAmount),
                        g.Count()
                    ))
                    .OrderByDescending(x => x.Period)
                    .Take(12)
                    .OrderBy(x => x.Period)
                    .ToList();
            }
            else
            {
                var raw = await query
                    .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Revenue = g.Sum(o => o.TotalAmount), Count = g.Count() })
                    .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month)
                    .Take(12)
                    .ToListAsync(ct);

                trends = raw.OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .Select(x => new RevenueTrendDto($"{x.Year}-{x.Month:D2}", x.Revenue, x.Count))
                    .ToList();
            }

            return ApiResult.Success(trends);
        });

        group.MapGet("/order-trends", async (
            ECommerceContext db,
            string? period = "month",
            CancellationToken ct = default) =>
        {
            var query = db.Orders.AsQueryable();

            List<RevenueTrendDto> trends;
            if (period == "day")
            {
                var raw = await query
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Date)
                    .Take(30)
                    .ToListAsync(ct);

                trends = raw.OrderBy(x => x.Date)
                    .Select(x => new RevenueTrendDto(x.Date.ToString("yyyy-MM-dd"), 0, x.Count))
                    .ToList();
            }
            else if (period == "week")
            {
                var all = await query
                    .Select(o => new { o.OrderDate })
                    .ToListAsync(ct);

                trends = all
                    .GroupBy(o => GetWeekStart(o.OrderDate))
                    .Select(g => new RevenueTrendDto(
                        g.Key.ToString("yyyy-MM-dd"),
                        0,
                        g.Count()
                    ))
                    .OrderByDescending(x => x.Period)
                    .Take(12)
                    .OrderBy(x => x.Period)
                    .ToList();
            }
            else
            {
                var raw = await query
                    .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                    .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month)
                    .Take(12)
                    .ToListAsync(ct);

                trends = raw.OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .Select(x => new RevenueTrendDto($"{x.Year}-{x.Month:D2}", 0, x.Count))
                    .ToList();
            }

            return ApiResult.Success(trends);
        });

        group.MapGet("/top-products", async (
            ECommerceContext db,
            int top = 10,
            CancellationToken ct = default) =>
        {
            var paidOrderIds = await db.Orders
                .Where(o => o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Paid)
                .Select(o => o.Id)
                .ToListAsync(ct);

            var topProducts = paidOrderIds.Count > 0
                ? await db.OrderItems
                    .Where(oi => paidOrderIds.Contains(oi.OrderId))
                    .GroupBy(oi => new { oi.ProductId, oi.ProductName })
                    .Select(g => new TopProductDto(
                        g.Key.ProductId,
                        g.Key.ProductName,
                        g.Sum(oi => oi.Quantity),
                        g.Sum(oi => oi.TotalPrice)
                    ))
                    .OrderByDescending(x => x.TotalSold)
                    .Take(top)
                    .ToListAsync(ct)
                : new List<TopProductDto>();

            return ApiResult.Success(topProducts);
        });

        group.MapGet("/low-stock", async (
            ECommerceContext db,
            int threshold = 5,
            CancellationToken ct = default) =>
        {
            var lowStock = await db.Products.IgnoreQueryFilters()
                .Where(p => p.StockQuantity <= threshold && !p.IsDeleted)
                .OrderBy(p => p.StockQuantity)
                .Select(p => new LowStockProductDto(p.Id, p.Name, p.StockQuantity))
                .ToListAsync(ct);

            return ApiResult.Success(lowStock);
        });

        group.MapGet("/user-growth", async (
            ECommerceContext db,
            string? period = "month",
            CancellationToken ct = default) =>
        {
            List<UserGrowthDto> growth;
            if (period == "day")
            {
                var raw = await db.Users
                    .GroupBy(u => u.CreatedAt.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Date)
                    .Take(30)
                    .ToListAsync(ct);

                growth = raw.OrderBy(x => x.Date)
                    .Select(x => new UserGrowthDto(x.Date.ToString("yyyy-MM-dd"), x.Count))
                    .ToList();
            }
            else if (period == "week")
            {
                var raw = await db.Users
                    .Select(u => new { u.CreatedAt })
                    .ToListAsync(ct);

                growth = raw
                    .GroupBy(u => GetWeekStart(u.CreatedAt))
                    .Select(g => new UserGrowthDto(g.Key.ToString("yyyy-MM-dd"), g.Count()))
                    .OrderByDescending(x => x.Period)
                    .Take(12)
                    .OrderBy(x => x.Period)
                    .ToList();
            }
            else
            {
                var raw = await db.Users
                    .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                    .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month)
                    .Take(12)
                    .ToListAsync(ct);

                growth = raw.OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .Select(x => new UserGrowthDto($"{x.Year}-{x.Month:D2}", x.Count))
                    .ToList();
            }

            return ApiResult.Success(growth);
        });

        group.MapGet("/payment-stats", async (
            ECommerceContext db,
            CancellationToken ct = default) =>
        {
            var stats = await db.Payments
                .GroupBy(p => p.Status)
                .Select(g => new PaymentStatsDto(
                    g.Key,
                    g.Count(),
                    g.Sum(p => p.Amount)
                ))
                .ToListAsync(ct);

            return ApiResult.Success(stats);
        });

        group.MapGet("/order-status-breakdown", async (
            ECommerceContext db,
            CancellationToken ct = default) =>
        {
            var breakdown = await db.Orders
                .GroupBy(o => o.Status)
                .Select(g => new OrderStatusBreakdownDto(
                    g.Key.ToString(),
                    g.Count()
                ))
                .ToListAsync(ct);

            return ApiResult.Success(breakdown);
        });

        group.MapGet("/review-stats", async (
            ECommerceContext db,
            CancellationToken ct = default) =>
        {
            var totalReviews = await db.Reviews.IgnoreQueryFilters().CountAsync(ct);
            var averageRating = totalReviews > 0
                ? await db.Reviews.IgnoreQueryFilters().AverageAsync(r => (double)r.Rating, ct)
                : 0.0;

            var distribution = await db.Reviews.IgnoreQueryFilters()
                .GroupBy(r => r.Rating)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var ratingDist = new Dictionary<int, int>();
            for (int i = 1; i <= 5; i++)
                ratingDist[i] = distribution.FirstOrDefault(d => d.Rating == i)?.Count ?? 0;

            return ApiResult.Success(new ReviewStatsDto(totalReviews, Math.Round(averageRating, 1), ratingDist));
        });

        group.MapGet("/full", async (
            ECommerceContext db,
            string? period = "month",
            CancellationToken ct = default) =>
        {
            var totalUsersTask = db.Users.CountAsync(ct);
            var ordersCountTask = db.Orders.CountAsync(ct);
            var productsCountTask = db.Products.IgnoreQueryFilters().CountAsync(ct);
            var revenueTask = db.Orders
                .Where(o => o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Paid)
                .SumAsync(o => (decimal?)o.TotalAmount);
            var lowStockCountTask = db.Products.IgnoreQueryFilters()
                .CountAsync(p => p.StockQuantity > 0 && p.StockQuantity <= 5, ct);
            var pendingOrdersTask = db.Orders
                .CountAsync(o => o.Status == OrderStatus.Pending, ct);
            var newUsersTodayTask = db.Users
                .CountAsync(u => u.CreatedAt >= DateTime.UtcNow.Date, ct);
            var reviewsCountTask = db.Reviews.IgnoreQueryFilters().CountAsync(ct);

            await Task.WhenAll(totalUsersTask, ordersCountTask, productsCountTask, revenueTask,
                lowStockCountTask, pendingOrdersTask, newUsersTodayTask, reviewsCountTask);

            var totalUsers = await totalUsersTask;
            var totalOrders = await ordersCountTask;
            var totalProducts = await productsCountTask;
            var totalRevenue = (await revenueTask) ?? 0;
            var lowStockProducts = await lowStockCountTask;
            var pendingOrders = await pendingOrdersTask;
            var newUsersToday = await newUsersTodayTask;
            var avgOrderValue = totalOrders > 0 ? Math.Round(totalRevenue / totalOrders, 2) : 0;
            var totalReviews = await reviewsCountTask;

            var overview = new AnalyticsOverviewDto(
                totalUsers, totalOrders, totalRevenue, totalProducts,
                lowStockProducts, pendingOrders, newUsersToday, avgOrderValue, totalReviews
            );

            var p = period ?? "month";

            var paidIds = await db.Orders
                .Where(o => o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Paid)
                .Select(o => o.Id)
                .ToListAsync(ct);

            var topProductsTask = Task.FromResult(new List<TopProductDto>());
            if (paidIds.Count > 0)
            {
                topProductsTask = db.OrderItems
                    .Where(oi => paidIds.Contains(oi.OrderId))
                    .GroupBy(oi => new { oi.ProductId, oi.ProductName })
                    .Select(g => new TopProductDto(g.Key.ProductId, g.Key.ProductName, g.Sum(oi => oi.Quantity), g.Sum(oi => oi.TotalPrice)))
                    .OrderByDescending(x => x.TotalSold)
                    .Take(10)
                    .ToListAsync(ct);
            }

            var lowStockTask = db.Products.IgnoreQueryFilters()
                .Where(p => p.StockQuantity <= 5 && !p.IsDeleted)
                .OrderBy(p => p.StockQuantity)
                .Select(p => new LowStockProductDto(p.Id, p.Name, p.StockQuantity))
                .ToListAsync(ct);

            var paymentStatsTask = db.Payments
                .GroupBy(p => p.Status)
                .Select(g => new PaymentStatsDto(g.Key, g.Count(), g.Sum(p => p.Amount)))
                .ToListAsync(ct);

            var orderStatusTask = db.Orders
                .GroupBy(o => o.Status)
                .Select(g => new OrderStatusBreakdownDto(g.Key.ToString(), g.Count()))
                .ToListAsync(ct);

            var reviewStatsTask = GetReviewStatsAsync(db, ct);

            List<RevenueTrendDto> revenueTrendsTask;
            List<RevenueTrendDto> orderTrendsTask;
            List<UserGrowthDto> userGrowthTask;

            if (p == "day")
            {
                var revenueRaw = await db.Orders
                    .Where(o => o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Paid)
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount), Count = g.Count() })
                    .OrderByDescending(x => x.Date).Take(30)
                    .ToListAsync(ct);
                revenueTrendsTask = revenueRaw.OrderBy(x => x.Date)
                    .Select(x => new RevenueTrendDto(x.Date.ToString("yyyy-MM-dd"), x.Revenue, x.Count)).ToList();

                var orderRaw = await db.Orders
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Date).Take(30)
                    .ToListAsync(ct);
                orderTrendsTask = orderRaw.OrderBy(x => x.Date)
                    .Select(x => new RevenueTrendDto(x.Date.ToString("yyyy-MM-dd"), 0, x.Count)).ToList();

                var userRaw = await db.Users
                    .GroupBy(u => u.CreatedAt.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Date).Take(30)
                    .ToListAsync(ct);
                userGrowthTask = userRaw.OrderBy(x => x.Date)
                    .Select(x => new UserGrowthDto(x.Date.ToString("yyyy-MM-dd"), x.Count)).ToList();
            }
            else
            {
                var revenueRaw = await db.Orders
                    .Where(o => o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Paid)
                    .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Revenue = g.Sum(o => o.TotalAmount), Count = g.Count() })
                    .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).Take(12)
                    .ToListAsync(ct);
                revenueTrendsTask = revenueRaw.OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .Select(x => new RevenueTrendDto($"{x.Year}-{x.Month:D2}", x.Revenue, x.Count)).ToList();

                var orderRaw = await db.Orders
                    .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                    .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).Take(12)
                    .ToListAsync(ct);
                orderTrendsTask = orderRaw.OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .Select(x => new RevenueTrendDto($"{x.Year}-{x.Month:D2}", 0, x.Count)).ToList();

                var userRaw = await db.Users
                    .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                    .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).Take(12)
                    .ToListAsync(ct);
                userGrowthTask = userRaw.OrderBy(x => x.Year).ThenBy(x => x.Month)
                    .Select(x => new UserGrowthDto($"{x.Year}-{x.Month:D2}", x.Count)).ToList();
            }

            await Task.WhenAll(topProductsTask, lowStockTask, paymentStatsTask, orderStatusTask, reviewStatsTask);

            return ApiResult.Success(new AnalyticsResponseDto(
                overview,
                revenueTrendsTask,
                orderTrendsTask,
                await topProductsTask,
                await lowStockTask,
                userGrowthTask,
                await paymentStatsTask,
                await orderStatusTask,
                await reviewStatsTask
            ));
        });
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }

    private static async Task<ReviewStatsDto> GetReviewStatsAsync(ECommerceContext db, CancellationToken ct)
    {
        var totalReviews = await db.Reviews.IgnoreQueryFilters().CountAsync(ct);
        var averageRating = totalReviews > 0
            ? await db.Reviews.IgnoreQueryFilters().AverageAsync(r => (double)r.Rating, ct)
            : 0.0;

        var distribution = await db.Reviews.IgnoreQueryFilters()
            .GroupBy(r => r.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var ratingDist = new Dictionary<int, int>();
        for (int i = 1; i <= 5; i++)
            ratingDist[i] = distribution.FirstOrDefault(d => d.Rating == i)?.Count ?? 0;

        return new ReviewStatsDto(totalReviews, Math.Round(averageRating, 1), ratingDist);
    }
}
