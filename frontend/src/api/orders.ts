import apiClient from './client';
import type { ApiResponse } from '@/types';
import type { OrderDto } from '@/types/order';

export const ordersApi = {
  async list() {
    const { data } = await apiClient.get<ApiResponse<OrderDto[]>>('/orders');
    return data;
  },

  async getById(id: number) {
    const { data } = await apiClient.get<ApiResponse<OrderDto>>(
      `/orders/${id}`
    );
    return data;
  },

  async checkout() {
    const { data } = await apiClient.post<
      ApiResponse<{ orderId: number; totalAmount: number }>
    >('/orders/checkout');
    return data;
  },

  async cancel(id: number) {
    const { data } = await apiClient.put<ApiResponse<{ message: string }>>(
      `/orders/${id}/cancel`
    );
    return data;
  },

  async updateStatus(id: number, newStatus: string) {
    const { data } = await apiClient.put<ApiResponse<{ message: string }>>(
      `/orders/${id}/status?newStatus=${newStatus}`
    );
    return data;
  },
};
