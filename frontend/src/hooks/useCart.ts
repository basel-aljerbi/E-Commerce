import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { cartApi } from '@/api/cart';
import { useAuthStore } from '@/store/auth';
import { useCartStore } from '@/store/cart';
import type { AddToCartDto, UpdateCartItemDto } from '@/types/cart';
import { AxiosError } from 'axios';
import type { ApiResponse } from '@/types';
import { toast } from 'sonner';

export function useCart() {
  const userId = useAuthStore((s) => s.user?.id);
  const syncFromServer = useCartStore((s) => s.syncFromServer);
  return useQuery({
    queryKey: ['cart', userId],
    queryFn: async () => {
      const result = await cartApi.get();
      if (result.success && result.data) {
        syncFromServer(result.data.items);
      }
      return result;
    },
    enabled: !!userId,
  });
}

function getErrorMessage(error: unknown): string {
  if (error instanceof AxiosError) {
    const apiMsg = (error.response?.data as ApiResponse)?.message;
    if (apiMsg) return apiMsg;
  }
  return '';
}

export function useAddToCart() {
  const qc = useQueryClient();
  const userId = useAuthStore((s) => s.user?.id);
  return useMutation({
    mutationFn: (dto: AddToCartDto) => cartApi.add(dto),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['cart', userId] });
      toast.success('Added to cart');
    },
    onError: (error) => {
      const msg = getErrorMessage(error) || 'Failed to add to cart';
      toast.error(msg);
    },
  });
}

export function useUpdateCartItem() {
  const qc = useQueryClient();
  const userId = useAuthStore((s) => s.user?.id);
  return useMutation({
    mutationFn: ({ id, dto }: { id: number; dto: UpdateCartItemDto }) =>
      cartApi.update(id, dto),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['cart', userId] }),
    onError: (error) => {
      const msg = getErrorMessage(error) || 'Failed to update cart';
      toast.error(msg);
    },
  });
}

export function useRemoveCartItem() {
  const qc = useQueryClient();
  const userId = useAuthStore((s) => s.user?.id);
  return useMutation({
    mutationFn: (id: number) => cartApi.remove(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['cart', userId] });
      toast.success('Item removed from cart');
    },
    onError: (error) => {
      const msg = getErrorMessage(error) || 'Failed to remove item';
      toast.error(msg);
    },
  });
}

export function useClearCart() {
  const qc = useQueryClient();
  const userId = useAuthStore((s) => s.user?.id);
  return useMutation({
    mutationFn: () => cartApi.clear(),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['cart', userId] });
      toast.success('Cart cleared');
    },
    onError: (error) => {
      const msg = getErrorMessage(error) || 'Failed to clear cart';
      toast.error(msg);
    },
  });
}
