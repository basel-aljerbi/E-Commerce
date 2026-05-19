import apiClient from './client';
import type { ApiResponse } from '@/types';
import type { User } from '@/types/auth';

export interface UpdateProfileData {
  fullName: string;
  phoneNumber?: string | null;
  addressLine1?: string | null;
  addressLine2?: string | null;
  city?: string | null;
  state?: string | null;
  postalCode?: string | null;
  country?: string | null;
}

export interface ChangePasswordData {
  currentPassword: string;
  newPassword: string;
}

export const profileApi = {
  async getProfile() {
    const { data } = await apiClient.get<ApiResponse<User>>('/profile');
    return data;
  },

  async updateProfile(dto: UpdateProfileData) {
    const { data } = await apiClient.put<ApiResponse<User>>('/profile', dto);
    return data;
  },

  async changePassword(dto: ChangePasswordData) {
    const { data } = await apiClient.put<ApiResponse<{ message: string }>>('/profile/password', dto);
    return data;
  },

  async uploadAvatar(file: File) {
    const formData = new FormData();
    formData.append('file', file);
    const { data } = await apiClient.post<ApiResponse<{ avatarUrl: string }>>('/profile/avatar', formData);
    return data;
  },
};
