import apiClient from './client';
import type { ApiResponse, PagedResult } from '@/types';
import type { AuditLogDto, AuditLogQueryParams } from '@/types/audit';
import type { ProductDto, CreateProductDto } from '@/types/product';
import type { OrderDto } from '@/types/order';
import type { User } from '@/types/auth';

export interface PaymentDto {
  id: number;
  orderId: number;
  amount: number;
  currency: string;
  status: string;
  stripePaymentIntentId: string | null;
  createdAt: string;
}

export interface DashboardData {
  totalProducts: number;
  totalOrders: number;
  totalRevenue: number;
  totalUsers: number;
  recentOrders: OrderDto[];
  revenueByMonth: Array<{ month: string; revenue: number }>;
}

export const adminApi = {
  async getDashboard() {
    const { data } = await apiClient.get<ApiResponse<DashboardData>>('/admin/dashboard');
    return data;
  },

  async getProducts(params?: {
    pageNumber?: number;
    pageSize?: number;
    search?: string;
  }) {
    const { data } = await apiClient.get<ApiResponse<PagedResult<ProductDto>>>('/admin/products', { params });
    return data;
  },

  async createProduct(dto: CreateProductDto) {
    const formData = new FormData();
    formData.append('Name', dto.name);
    formData.append('Description', dto.description);
    formData.append('Price', dto.price.toString());
    formData.append('StockQuantity', dto.stockQuantity.toString());
    formData.append('CategoryId', dto.categoryId.toString());
    if (dto.image) formData.append('Image', dto.image);

    const { data } = await apiClient.post<ApiResponse<{ id: number }>>(
      '/admin/products',
      formData,
    );
    return data;
  },

  async updateProduct(id: number, dto: CreateProductDto) {
    const formData = new FormData();
    formData.append('Name', dto.name);
    formData.append('Description', dto.description);
    formData.append('Price', dto.price.toString());
    formData.append('StockQuantity', dto.stockQuantity.toString());
    formData.append('CategoryId', dto.categoryId.toString());
    if (dto.image) formData.append('Image', dto.image);

    await apiClient.put(`/admin/products/${id}`, formData);
  },

  async deleteProduct(id: number) {
    await apiClient.delete(`/admin/products/${id}`);
  },

  async getOrders(params?: {
    pageNumber?: number;
    pageSize?: number;
    status?: string;
  }) {
    const { data } = await apiClient.get<ApiResponse<PagedResult<OrderDto>>>('/admin/orders', { params });
    return data;
  },

  async updateOrderStatus(id: number, newStatus: string) {
    const { data } = await apiClient.put<ApiResponse<{ message: string }>>(`/admin/orders/${id}/status?newStatus=${newStatus}`);
    return data;
  },

  async getUsers(params?: {
    pageNumber?: number;
    pageSize?: number;
    search?: string;
  }) {
    const { data } = await apiClient.get<ApiResponse<PagedResult<User>>>('/admin/users', { params });
    return data;
  },

  async updateUserRole(id: number, role: string) {
    const { data } = await apiClient.put<ApiResponse<{ message: string }>>(`/admin/users/${id}`, { role });
    return data;
  },

  async getAuditLogs(params?: AuditLogQueryParams) {
    const { data } = await apiClient.get<ApiResponse<PagedResult<AuditLogDto>>>('/audit-logs', { params });
    return data;
  },

  async getPayments(params?: {
    pageNumber?: number;
    pageSize?: number;
  }) {
    const { data } = await apiClient.get<ApiResponse<PagedResult<PaymentDto>>>('/admin/payments', { params });
    return data;
  },

  async getCategories() {
    const { data } = await apiClient.get<ApiResponse<Array<{ id: number; name: string; imageUrl: string | null }>>>('/categories');
    return data;
  },

  async createCategory(name: string, image?: File) {
    const formData = new FormData();
    formData.append('Name', name);
    if (image) formData.append('Image', image);
    const { data } = await apiClient.post<ApiResponse<{ id: number; name: string; imageUrl: string | null }>>('/admin/categories', formData);
    return data;
  },

  async updateCategory(id: number, name: string, image?: File | null) {
    const formData = new FormData();
    formData.append('Name', name);
    if (image) formData.append('Image', image);
    await apiClient.put(`/admin/categories/${id}`, formData);
  },

  async deleteCategory(id: number) {
    await apiClient.delete(`/admin/categories/${id}`);
  },
};
