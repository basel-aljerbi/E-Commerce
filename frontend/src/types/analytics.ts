export interface AnalyticsOverview {
  totalUsers: number;
  totalOrders: number;
  totalRevenue: number;
  totalProducts: number;
  lowStockProducts: number;
  pendingOrders: number;
  newUsersToday: number;
  averageOrderValue: number;
  totalReviews: number;
}

export interface RevenueTrend {
  period: string;
  revenue: number;
  orderCount: number;
}

export interface TopProduct {
  productId: number;
  productName: string;
  totalSold: number;
  totalRevenue: number;
}

export interface LowStockProduct {
  productId: number;
  productName: string;
  stockQuantity: number;
}

export interface UserGrowth {
  period: string;
  newUsers: number;
}

export interface PaymentStats {
  status: string;
  count: number;
  totalAmount: number;
}

export interface OrderStatusBreakdown {
  status: string;
  count: number;
}

export interface ReviewStats {
  totalReviews: number;
  averageRating: number;
  ratingDistribution: Record<number, number>;
}

export interface AnalyticsResponse {
  overview: AnalyticsOverview;
  revenueTrends: RevenueTrend[];
  orderTrends: RevenueTrend[];
  topProducts: TopProduct[];
  lowStockProducts: LowStockProduct[];
  userGrowth: UserGrowth[];
  paymentStats: PaymentStats[];
  orderStatusBreakdown: OrderStatusBreakdown[];
  reviewStats: ReviewStats;
}
