import { useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { analyticsApi } from '@/api/analytics';

export function useAnalyticsFull(period: string = 'month') {
  return useQuery({
    queryKey: ['admin', 'analytics', 'full', period],
    queryFn: () => analyticsApi.getFull(period),
    staleTime: 60_000,
  });
}

export function useAnalyticsOverview() {
  return useQuery({
    queryKey: ['admin', 'analytics', 'overview'],
    queryFn: () => analyticsApi.getOverview(),
    staleTime: 60_000,
  });
}

export function useAnalyticsTopProducts(top: number = 10) {
  return useQuery({
    queryKey: ['admin', 'analytics', 'top-products', top],
    queryFn: () => analyticsApi.getTopProducts(top),
    staleTime: 60_000,
  });
}

export function useAnalyticsLowStock(threshold: number = 5) {
  return useQuery({
    queryKey: ['admin', 'analytics', 'low-stock', threshold],
    queryFn: () => analyticsApi.getLowStock(threshold),
    staleTime: 30_000,
  });
}

export function useAnalyticsReviewStats() {
  return useQuery({
    queryKey: ['admin', 'analytics', 'review-stats'],
    queryFn: () => analyticsApi.getReviewStats(),
    staleTime: 60_000,
  });
}

export function useOrderStatusBreakdown() {
  return useQuery({
    queryKey: ['admin', 'analytics', 'order-status'],
    queryFn: () => analyticsApi.getOrderStatusBreakdown(),
    staleTime: 60_000,
  });
}

export function usePaymentStats() {
  return useQuery({
    queryKey: ['admin', 'analytics', 'payment-stats'],
    queryFn: () => analyticsApi.getPaymentStats(),
    staleTime: 60_000,
  });
}

export function useMemoizedStats(data: any) {
  const overview = useMemo(() => data?.data?.overview ?? null, [data]);
  const revenueTrends = useMemo(() => data?.data?.revenueTrends ?? [], [data]);
  const orderTrends = useMemo(() => data?.data?.orderTrends ?? [], [data]);
  const topProducts = useMemo(() => data?.data?.topProducts ?? [], [data]);
  const lowStock = useMemo(() => data?.data?.lowStockProducts ?? [], [data]);
  const userGrowth = useMemo(() => data?.data?.userGrowth ?? [], [data]);
  const paymentStats = useMemo(() => data?.data?.paymentStats ?? [], [data]);
  const orderStatusBreakdown = useMemo(() => data?.data?.orderStatusBreakdown ?? [], [data]);
  const reviewStats = useMemo(() => data?.data?.reviewStats ?? null, [data]);

  return { overview, revenueTrends, orderTrends, topProducts, lowStock, userGrowth, paymentStats, orderStatusBreakdown, reviewStats };
}
