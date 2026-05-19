import apiClient from './client';
import type { ApiResponse } from '@/types';
import type {
  AnalyticsOverview,
  RevenueTrend,
  TopProduct,
  LowStockProduct,
  UserGrowth,
  PaymentStats,
  OrderStatusBreakdown,
  ReviewStats,
  AnalyticsResponse,
} from '@/types/analytics';

export const analyticsApi = {
  async getOverview() {
    const { data } = await apiClient.get<ApiResponse<AnalyticsOverview>>('/admin/analytics/overview');
    return data;
  },

  async getRevenueTrends(period: string = 'month') {
    const { data } = await apiClient.get<ApiResponse<RevenueTrend[]>>('/admin/analytics/revenue-trends', { params: { period } });
    return data;
  },

  async getOrderTrends(period: string = 'month') {
    const { data } = await apiClient.get<ApiResponse<RevenueTrend[]>>('/admin/analytics/order-trends', { params: { period } });
    return data;
  },

  async getTopProducts(top: number = 10) {
    const { data } = await apiClient.get<ApiResponse<TopProduct[]>>('/admin/analytics/top-products', { params: { top } });
    return data;
  },

  async getLowStock(threshold: number = 5) {
    const { data } = await apiClient.get<ApiResponse<LowStockProduct[]>>('/admin/analytics/low-stock', { params: { threshold } });
    return data;
  },

  async getUserGrowth(period: string = 'month') {
    const { data } = await apiClient.get<ApiResponse<UserGrowth[]>>('/admin/analytics/user-growth', { params: { period } });
    return data;
  },

  async getPaymentStats() {
    const { data } = await apiClient.get<ApiResponse<PaymentStats[]>>('/admin/analytics/payment-stats');
    return data;
  },

  async getOrderStatusBreakdown() {
    const { data } = await apiClient.get<ApiResponse<OrderStatusBreakdown[]>>('/admin/analytics/order-status-breakdown');
    return data;
  },

  async getReviewStats() {
    const { data } = await apiClient.get<ApiResponse<ReviewStats>>('/admin/analytics/review-stats');
    return data;
  },

  async getFull(period: string = 'month') {
    const { data } = await apiClient.get<ApiResponse<AnalyticsResponse>>('/admin/analytics/full', { params: { period } });
    return data;
  },
};
