import apiClient from './client';
import type { ApiResponse, PagedResult } from '@/types';
import type {
  ProductDto,
  ProductQueryParams,
  CreateProductDto,
  Category,
} from '@/types/product';

export const productsApi = {
  async list(params?: ProductQueryParams) {
    const { data } = await apiClient.get<ApiResponse<PagedResult<ProductDto>>>(
      '/products',
      { params }
    );
    return data;
  },

  async getById(id: number) {
    const { data } = await apiClient.get<ApiResponse<ProductDto>>(
      `/products/${id}`
    );
    return data;
  },

  async create(dto: CreateProductDto) {
    const formData = new FormData();
    formData.append('Name', dto.name);
    formData.append('Description', dto.description);
    formData.append('Price', dto.price.toString());
    formData.append('StockQuantity', dto.stockQuantity.toString());
    formData.append('CategoryId', dto.categoryId.toString());
    if (dto.image) formData.append('Image', dto.image);

    const { data } = await apiClient.post<ApiResponse<{ id: number }>>(
      '/products',
      formData,
      { headers: { 'Content-Type': 'multipart/form-data' } }
    );
    return data;
  },

  async update(id: number, dto: CreateProductDto) {
    const formData = new FormData();
    formData.append('Name', dto.name);
    formData.append('Description', dto.description);
    formData.append('Price', dto.price.toString());
    formData.append('StockQuantity', dto.stockQuantity.toString());
    formData.append('CategoryId', dto.categoryId.toString());
    if (dto.image) formData.append('Image', dto.image);

    await apiClient.put(`/products/${id}`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  async delete(id: number) {
    await apiClient.delete(`/products/${id}`);
  },

  async getCategories() {
    // Note: backend returns categories as part of product data;
    // we call a special endpoint or rely on cached data
    const { data } = await apiClient.get<ApiResponse<Category[]>>(
      '/categories'
    );
    return data;
  },
};
