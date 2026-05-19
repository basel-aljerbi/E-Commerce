import apiClient from './client';
import type { ApiResponse } from '@/types';
import type { ReviewsResponse, CreateReviewDto } from '@/types/review';

export const reviewsApi = {
  async getByProduct(productId: number) {
    const { data } = await apiClient.get<ApiResponse<ReviewsResponse>>(
      `/products/${productId}/reviews`
    );
    return data;
  },

  async create(productId: number, dto: CreateReviewDto) {
    const { data } = await apiClient.post<ApiResponse<{ id: number }>>(
      `/products/${productId}/reviews`,
      dto
    );
    return data;
  },

  async delete(productId: number, reviewId: number) {
    await apiClient.delete(`/products/${productId}/reviews/${reviewId}`);
  },
};
