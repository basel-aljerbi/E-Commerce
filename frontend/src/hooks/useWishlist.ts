import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { wishlistApi } from '@/api/wishlist';
import { useAuthStore } from '@/store/auth';
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
  return useMutation({
    mutationFn: (productId: number) => wishlistApi.add(productId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['wishlist'] });
      toast.success('Added to wishlist');
    },
    onError: () => toast.error('Failed to add to wishlist'),
  });
}

export function useRemoveFromWishlist() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (productId: number) => wishlistApi.remove(productId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['wishlist'] });
      toast.success('Removed from wishlist');
    },
    onError: () => toast.error('Failed to remove from wishlist'),
  });
}
