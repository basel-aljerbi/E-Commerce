import { describe, it, expect, vi, beforeEach } from 'vitest';
import { analyticsApi } from '@/api/analytics';

vi.mock('@/api/client', () => ({
  default: {
    get: vi.fn(),
  },
}));

import apiClient from '@/api/client';

describe('analyticsApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('getOverview calls GET /admin/analytics/overview', async () => {
    (apiClient.get as any).mockResolvedValue({ data: { data: { totalRevenue: 1000 } } });
    const res = await analyticsApi.getOverview();
    expect(apiClient.get).toHaveBeenCalledWith('/admin/analytics/overview');
    expect(res.data.totalRevenue).toBe(1000);
  });

  it('getRevenueTrends calls with period param', async () => {
    (apiClient.get as any).mockResolvedValue({ data: { data: [] } });
    await analyticsApi.getRevenueTrends('day');
    expect(apiClient.get).toHaveBeenCalledWith('/admin/analytics/revenue-trends', { params: { period: 'day' } });
  });

  it('getTopProducts calls with top param', async () => {
    (apiClient.get as any).mockResolvedValue({ data: { data: [] } });
    await analyticsApi.getTopProducts(5);
    expect(apiClient.get).toHaveBeenCalledWith('/admin/analytics/top-products', { params: { top: 5 } });
  });

  it('getLowStock calls with threshold param', async () => {
    (apiClient.get as any).mockResolvedValue({ data: { data: [] } });
    await analyticsApi.getLowStock(3);
    expect(apiClient.get).toHaveBeenCalledWith('/admin/analytics/low-stock', { params: { threshold: 3 } });
  });

  it('getFull calls with period param', async () => {
    (apiClient.get as any).mockResolvedValue({ data: { data: {} } });
    await analyticsApi.getFull('month');
    expect(apiClient.get).toHaveBeenCalledWith('/admin/analytics/full', { params: { period: 'month' } });
  });

  it('getOrderTrends calls /admin/analytics/order-trends', async () => {
    (apiClient.get as any).mockResolvedValue({ data: { data: [] } });
    await analyticsApi.getOrderTrends('month');
    expect(apiClient.get).toHaveBeenCalledWith('/admin/analytics/order-trends', { params: { period: 'month' } });
  });

  it('getUserGrowth calls /admin/analytics/user-growth', async () => {
    (apiClient.get as any).mockResolvedValue({ data: { data: [] } });
    await analyticsApi.getUserGrowth('month');
    expect(apiClient.get).toHaveBeenCalledWith('/admin/analytics/user-growth', { params: { period: 'month' } });
  });

  it('getPaymentStats calls /admin/analytics/payment-stats', async () => {
    (apiClient.get as any).mockResolvedValue({ data: { data: [] } });
    await analyticsApi.getPaymentStats();
    expect(apiClient.get).toHaveBeenCalledWith('/admin/analytics/payment-stats');
  });

  it('getOrderStatusBreakdown calls /admin/analytics/order-status-breakdown', async () => {
    (apiClient.get as any).mockResolvedValue({ data: { data: [] } });
    await analyticsApi.getOrderStatusBreakdown();
    expect(apiClient.get).toHaveBeenCalledWith('/admin/analytics/order-status-breakdown');
  });

  it('getReviewStats calls /admin/analytics/review-stats', async () => {
    (apiClient.get as any).mockResolvedValue({ data: { data: {} } });
    await analyticsApi.getReviewStats();
    expect(apiClient.get).toHaveBeenCalledWith('/admin/analytics/review-stats');
  });
});
