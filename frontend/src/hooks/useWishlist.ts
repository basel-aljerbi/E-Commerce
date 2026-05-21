import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { wishlistApi } from '@/api/wishlist';
import { useAuthStore } from '@/store/auth';
import { AxiosError } from 'axios';
import type { ApiResponse } from '@/types';
import { toast } from 'sonner';

export function useWishlist() {
  const userId = useAuthStore((s) => s.user?.id);
  return useQuery({
    queryKey: ['wishlist', userId],
    queryFn: () => wishlistApi.get(),
    enabled: !!userId,
  });
}

export function useAddToWishlist() {
  const qc = useQueryClient();
  const userId = useAuthStore((s) => s.user?.id);
  return useMutation({
    mutationFn: (productId: number) => wishlistApi.add(productId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['wishlist', userId] });
      toast.success('Added to wishlist');
    },
    onError: (error) => {
      const msg =
        (error instanceof AxiosError &&
          (error.response?.data as ApiResponse)?.message) ||
        'Failed to add to wishlist';
      toast.error(msg);
    },
  });
}

export function useRemoveFromWishlist() {
  const qc = useQueryClient();
  const userId = useAuthStore((s) => s.user?.id);
  return useMutation({
    mutationFn: (productId: number) => wishlistApi.remove(productId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['wishlist', userId] });
      toast.success('Removed from wishlist');
    },
    onError: (error) => {
      const msg =
        (error instanceof AxiosError &&
          (error.response?.data as ApiResponse)?.message) ||
        'Failed to remove from wishlist';
      toast.error(msg);
    },
  });
}
