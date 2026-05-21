import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { cartApi } from '@/api/cart';
import { useAuthStore } from '@/store/auth';
import { useCartStore } from '@/store/cart';
import type { AddToCartDto, UpdateCartItemDto } from '@/types/cart';
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

export function useAddToCart() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (dto: AddToCartDto) => cartApi.add(dto),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['cart'] });
      toast.success('Added to cart');
    },
    onError: () => toast.error('Failed to add to cart'),
  });
}

export function useUpdateCartItem() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, dto }: { id: number; dto: UpdateCartItemDto }) =>
      cartApi.update(id, dto),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['cart'] }),
  });
}

export function useRemoveCartItem() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => cartApi.remove(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['cart'] });
      toast.success('Item removed from cart');
    },
  });
}

export function useClearCart() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => cartApi.clear(),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['cart'] });
      toast.success('Cart cleared');
    },
  });
}
