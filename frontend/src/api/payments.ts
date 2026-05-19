import apiClient from './client';
import type { ApiResponse } from '@/types';
import type { PaymentRequestDto, PaymentResponseDto } from '@/types/payment';

export const paymentsApi = {
  async createIntent(dto: PaymentRequestDto) {
    const { data } = await apiClient.post<ApiResponse<PaymentResponseDto>>(
      '/payments/create-intent',
      dto
    );
    return data;
  },
};
