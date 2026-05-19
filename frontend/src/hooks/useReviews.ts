import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { reviewsApi } from '@/api/reviews';
import type { CreateReviewDto } from '@/types/review';
import { toast } from 'sonner';

export function useReviews(productId: number) {
  return useQuery({
    queryKey: ['reviews', productId],
    queryFn: () => reviewsApi.getByProduct(productId),
    enabled: !!productId,
  });
}

export function useCreateReview(productId: number) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (dto: CreateReviewDto) => reviewsApi.create(productId, dto),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['reviews', productId] });
      toast.success('Review submitted');
    },
    onError: () => toast.error('Failed to submit review'),
  });
}

export function useDeleteReview() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      productId,
      reviewId,
    }: {
      productId: number;
      reviewId: number;
    }) => reviewsApi.delete(productId, reviewId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['reviews'] });
      toast.success('Review deleted');
    },
    onError: () => toast.error('Failed to delete review'),
  });
}
