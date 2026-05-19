namespace E_Commerce.API.Dtos;

public record AnalyticsOverviewDto(
    int TotalUsers,
    int TotalOrders,
    decimal TotalRevenue,
    int TotalProducts,
    int LowStockProducts,
    int PendingOrders,
    int NewUsersToday,
    decimal AverageOrderValue,
    int TotalReviews
);

public record RevenueTrendDto(
    string Period,
    decimal Revenue,
    int OrderCount
);

public record TopProductDto(
    int ProductId,
    string ProductName,
    int TotalSold,
    decimal TotalRevenue
);

public record LowStockProductDto(
    int ProductId,
    string ProductName,
    int StockQuantity
);

public record UserGrowthDto(
    string Period,
    int NewUsers
);

public record PaymentStatsDto(
    string Status,
    int Count,
    decimal TotalAmount
);

public record OrderStatusBreakdownDto(
    string Status,
    int Count
);

public record AnalyticsResponseDto(
    AnalyticsOverviewDto Overview,
    List<RevenueTrendDto> RevenueTrends,
    List<RevenueTrendDto> OrderTrends,
    List<TopProductDto> TopProducts,
    List<LowStockProductDto> LowStockProducts,
    List<UserGrowthDto> UserGrowth,
    List<PaymentStatsDto> PaymentStats,
    List<OrderStatusBreakdownDto> OrderStatusBreakdown,
    ReviewStatsDto ReviewStats
);

public record ReviewStatsDto(
    int TotalReviews,
    double AverageRating,
    Dictionary<int, int> RatingDistribution
);
