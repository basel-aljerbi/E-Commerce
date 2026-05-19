import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { adminApi } from '@/api/admin';
import type { AuditLogQueryParams } from '@/types/audit';
import type { CreateProductDto } from '@/types/product';
import { toast } from 'sonner';
import { isAxiosError } from 'axios';
import type { ProblemDetails, ApiResponse } from '@/types';

function getApiError(error: unknown): string {
  console.error('[Admin] API Error:', error);
  if (isAxiosError<ProblemDetails | ApiResponse>(error)) {
    const data = error.response?.data;
    console.log('[Admin] Response body:', JSON.stringify(data));
    if (data) {
      if ('message' in data && data.message) return data.message;
      if ('errors' in data && data.errors) {
        const entries = Object.entries(data.errors);
        if (entries.length > 0) {
          const [field, msgs] = entries[0];
          return `${field}: ${msgs[0]}`;
        }
      }
      if ('title' in data && data.title) return data.title;
    }
    return `HTTP ${error.response?.status ?? '??'} ${error.response?.statusText ?? 'Network error'}`;
  }
  return error instanceof Error ? error.message : 'An unexpected error occurred';
}

export function useAdminDashboard() {
  return useQuery({
    queryKey: ['admin', 'dashboard'],
    queryFn: () => adminApi.getDashboard(),
  });
}

export function useAdminProducts(params?: {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
}) {
  return useQuery({
    queryKey: ['admin', 'products', params],
    queryFn: () => adminApi.getProducts(params),
  });
}

export function useAdminCreateProduct() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (dto: CreateProductDto) => adminApi.createProduct(dto),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'products'] });
      toast.success('Product created');
    },
    onError: (error) => toast.error(getApiError(error)),
  });
}

export function useAdminUpdateProduct(id: number) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (dto: CreateProductDto) => adminApi.updateProduct(id, dto),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'products'] });
      toast.success('Product updated');
    },
    onError: (error) => toast.error(getApiError(error)),
  });
}

export function useAdminDeleteProduct() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => adminApi.deleteProduct(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'products'] });
      toast.success('Product deleted');
    },
    onError: (error) => toast.error(getApiError(error)),
  });
}

export function useAdminOrders(params?: {
  pageNumber?: number;
  pageSize?: number;
  status?: string;
}) {
  return useQuery({
    queryKey: ['admin', 'orders', params],
    queryFn: () => adminApi.getOrders(params),
  });
}

export function useAdminUpdateOrderStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, status }: { id: number; status: string }) =>
      adminApi.updateOrderStatus(id, status),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'orders'] });
      toast.success('Order status updated');
    },
    onError: (error) => toast.error(getApiError(error)),
  });
}

export function useAdminUsers(params?: {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
}) {
  return useQuery({
    queryKey: ['admin', 'users', params],
    queryFn: () => adminApi.getUsers(params),
  });
}

export function useAdminAuditLogs(params?: AuditLogQueryParams) {
  return useQuery({
    queryKey: ['admin', 'audit-logs', params],
    queryFn: () => adminApi.getAuditLogs(params),
  });
}

export function useAdminPayments(params?: {
  pageNumber?: number;
  pageSize?: number;
}) {
  return useQuery({
    queryKey: ['admin', 'payments', params],
    queryFn: () => adminApi.getPayments(params),
  });
}

export function useAdminCategories() {
  return useQuery({
    queryKey: ['admin', 'categories'],
    queryFn: () => adminApi.getCategories(),
  });
}

export function useAdminCreateCategory() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ name, image }: { name: string; image?: File }) =>
      adminApi.createCategory(name, image),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'categories'] });
      toast.success('Category created');
    },
    onError: (error) => toast.error(getApiError(error)),
  });
}

export function useAdminUpdateCategory() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, name, image }: { id: number; name: string; image?: File | null }) =>
      adminApi.updateCategory(id, name, image),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'categories'] });
      toast.success('Category updated');
    },
    onError: (error) => toast.error(getApiError(error)),
  });
}

export function useAdminDeleteCategory() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => adminApi.deleteCategory(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'categories'] });
      toast.success('Category deleted');
    },
    onError: (error) => toast.error(getApiError(error)),
  });
}
