export interface WishlistItemDto {
  id: number;
  productId: number;
  productName: string;
  productPrice: number;
  imageUrl: string | null;
  addedAt: string;
}
