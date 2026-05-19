import type { ProductQueryParams } from '@/types';

export function buildProductQueryString(params: ProductQueryParams): string {
  const searchParams = new URLSearchParams();
  if (params.pageNumber) searchParams.set('pageNumber', String(params.pageNumber));
  if (params.pageSize) searchParams.set('pageSize', String(params.pageSize));
  if (params.categoryId) searchParams.set('categoryId', String(params.categoryId));
  if (params.search) searchParams.set('search', params.search);
  if (params.minPrice !== undefined) searchParams.set('minPrice', String(params.minPrice));
  if (params.maxPrice !== undefined) searchParams.set('maxPrice', String(params.maxPrice));
  if (params.sortBy) searchParams.set('sortBy', params.sortBy);
  if (params.sortOrder) searchParams.set('sortOrder', params.sortOrder);
  return searchParams.toString();
}
