export interface ReviewDto {
  id: number;
  productId: number;
  userName: string;
  rating: number;
  comment?: string;
  createdAt: string;
}

export interface ReviewsResponse {
  reviews: ReviewDto[];
  averageRating: number;
  totalReviews: number;
}

export interface CreateReviewDto {
  rating: number;
  comment?: string;
}
