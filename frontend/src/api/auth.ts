import apiClient from './client';
import type { ApiResponse, AuthResponseDto } from '@/types/auth';
import type { LoginDto, RegisterDto, ForgotPasswordDto, ResetPasswordDto, VerifyEmailDto } from '@/types/auth';

export const authApi = {
  async login(dto: LoginDto) {
    const { data } = await apiClient.post<ApiResponse<AuthResponseDto>>(
      '/auth/login',
      dto
    );
    return data;
  },

  async register(dto: RegisterDto) {
    const { data } = await apiClient.post<ApiResponse<{ message: string }>>(
      '/auth/register',
      dto
    );
    return data;
  },

  async verifyEmail(dto: VerifyEmailDto) {
    const { data } = await apiClient.post<ApiResponse<{ message: string }>>(
      '/auth/verify-email',
      dto
    );
    return data;
  },

  async refresh(refreshToken: string) {
    const { data } = await apiClient.post<ApiResponse<AuthResponseDto>>(
      '/auth/refresh',
      { refreshToken }
    );
    return data;
  },

  async logout() {
    const { data } = await apiClient.post<ApiResponse<{ message: string }>>(
      '/auth/logout'
    );
    return data;
  },

  async forgotPassword(dto: ForgotPasswordDto) {
    const { data } = await apiClient.post<ApiResponse<{ message: string }>>(
      '/auth/forgot-password',
      dto
    );
    return data;
  },

  async resetPassword(dto: ResetPasswordDto) {
    const { data } = await apiClient.post<ApiResponse<{ message: string }>>(
      '/auth/reset-password',
      dto
    );
    return data;
  },
};
