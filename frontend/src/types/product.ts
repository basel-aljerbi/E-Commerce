export interface ProductDto {
  id: number;
  name: string;
  description: string;
  price: number;
  stockQuantity: number;
  imageUrl: string | null;
  categoryName: string;
  categoryId?: number;
}

export interface CreateProductDto {
  name: string;
  description: string;
  price: number;
  stockQuantity: number;
  categoryId: number;
  image?: File;
}

export interface ProductQueryParams {
  pageNumber?: number;
  pageSize?: number;
  categoryId?: number;
  search?: string;
  minPrice?: number;
  maxPrice?: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

export interface Category {
  id: number;
  name: string;
  imageUrl: string | null;
}
