import apiClient from './client';
import type { ApiResponse } from '@/types';
import type {
  CartResponse,
  AddToCartDto,
  UpdateCartItemDto,
  CartItemDto,
} from '@/types/cart';

export const cartApi = {
  async get() {
    const { data } = await apiClient.get<ApiResponse<CartResponse>>('/cart');
    return data;
  },

  async add(dto: AddToCartDto) {
    const { data } = await apiClient.post<ApiResponse<{ message: string }>>(
      '/cart',
      dto
    );
    return data;
  },

  async update(id: number, dto: UpdateCartItemDto) {
    await apiClient.put(`/cart/${id}`, dto);
  },

  async remove(id: number) {
    await apiClient.delete(`/cart/${id}`);
  },

  async clear() {
    await apiClient.delete('/cart');
  },
};
