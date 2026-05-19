import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { profileApi, type UpdateProfileData, type ChangePasswordData } from '@/api/profile';
import { toast } from 'sonner';
import { isAxiosError } from 'axios';
import type { ProblemDetails, ApiResponse } from '@/types';

function getApiError(error: unknown): string {
  if (isAxiosError<ProblemDetails | ApiResponse>(error)) {
    const data = error.response?.data;
    if (data) {
      if ('message' in data && data.message) return data.message;
      if ('title' in data && data.title) return data.title;
    }
    return `HTTP ${error.response?.status ?? '??'}`;
  }
  return error instanceof Error ? error.message : 'An unexpected error occurred';
}

export function useProfile() {
  return useQuery({
    queryKey: ['profile'],
    queryFn: () => profileApi.getProfile(),
    staleTime: 30_000,
  });
}

export function useUpdateProfile() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (dto: UpdateProfileData) => profileApi.updateProfile(dto),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['profile'] });
      qc.invalidateQueries({ queryKey: ['auth'] });
      toast.success('Profile updated successfully');
    },
    onError: (error) => toast.error(getApiError(error)),
  });
}

export function useChangePassword() {
  return useMutation({
    mutationFn: (dto: ChangePasswordData) => profileApi.changePassword(dto),
    onSuccess: () => toast.success('Password changed successfully'),
    onError: (error) => toast.error(getApiError(error)),
  });
}

export function useUploadAvatar() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => profileApi.uploadAvatar(file),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['profile'] });
      toast.success('Avatar uploaded successfully');
    },
    onError: (error) => toast.error(getApiError(error)),
  });
}
