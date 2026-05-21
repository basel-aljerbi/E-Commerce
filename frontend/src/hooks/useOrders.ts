import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ordersApi } from '@/api/orders';
import { useAuthStore } from '@/store/auth';
import { toast } from 'sonner';
import { AxiosError } from 'axios';
import type { ApiResponse } from '@/types';

export function useOrders() {
  return useQuery({
    queryKey: ['orders'],
    queryFn: () => ordersApi.list(),
  });
}

export function useOrder(id: number) {
  return useQuery({
    queryKey: ['order', id],
    queryFn: () => ordersApi.getById(id),
    enabled: !!id,
  });
}

export function useCheckout() {
  const qc = useQueryClient();
  const userId = useAuthStore((s) => s.user?.id);
  return useMutation({
    mutationFn: () => ordersApi.checkout(),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['cart', userId] });
      qc.invalidateQueries({ queryKey: ['orders'] });
    },
    onError: (error) => {
      const msg =
        (error instanceof AxiosError &&
          (error.response?.data as ApiResponse)?.message) ||
        'Checkout failed';
      toast.error(msg);
    },
  });
}

export function useCancelOrder() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => ordersApi.cancel(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['orders'] });
      toast.success('Order cancelled');
    },
    onError: (error) => {
      const msg =
        (error instanceof AxiosError &&
          (error.response?.data as ApiResponse)?.message) ||
        'Failed to cancel order';
      toast.error(msg);
    },
  });
}
