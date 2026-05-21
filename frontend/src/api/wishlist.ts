import apiClient from './client';
import type { ApiResponse } from '@/types';
import type { WishlistItemDto } from '@/types/wishlist';

export const wishlistApi = {
  async get() {
    const { data } = await apiClient.get<ApiResponse<WishlistItemDto[]>>(
      '/wishlist'
    );
    return data;
  },

  async add(productId: number) {
    const { data } = await apiClient.post<ApiResponse<number>>(
      `/wishlist/${productId}`
    );
    return data;
  },

  async remove(productId: number) {
    await apiClient.delete(`/wishlist/${productId}`);
  },
};
