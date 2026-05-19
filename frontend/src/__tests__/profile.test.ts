import { describe, it, expect, vi, beforeEach } from 'vitest';
import { profileApi } from '@/api/profile';

vi.mock('@/api/client', () => ({
  default: {
    get: vi.fn(),
    put: vi.fn(),
    post: vi.fn(),
  },
}));

import apiClient from '@/api/client';

describe('profileApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('getProfile calls GET /profile', async () => {
    const mockResp = { data: { data: { id: '1', email: 'a@b.com', fullName: 'Test' } } };
    (apiClient.get as any).mockResolvedValue(mockResp);
    const res = await profileApi.getProfile();
    expect(apiClient.get).toHaveBeenCalledWith('/profile');
    expect(res.data.fullName).toBe('Test');
  });

  it('updateProfile calls PUT /profile', async () => {
    const mockResp = { data: { data: { fullName: 'Updated' } } };
    (apiClient.put as any).mockResolvedValue(mockResp);
    const res = await profileApi.updateProfile({ fullName: 'Updated' });
    expect(apiClient.put).toHaveBeenCalledWith('/profile', { fullName: 'Updated' });
    expect(res.data.fullName).toBe('Updated');
  });

  it('changePassword calls PUT /profile/password', async () => {
    (apiClient.put as any).mockResolvedValue({ data: { data: { message: 'OK' } } });
    await profileApi.changePassword({ currentPassword: 'old', newPassword: 'New1abc!' });
    expect(apiClient.put).toHaveBeenCalledWith('/profile/password', { currentPassword: 'old', newPassword: 'New1abc!' });
  });

  it('uploadAvatar calls POST /profile/avatar with FormData', async () => {
    (apiClient.post as any).mockResolvedValue({ data: { data: { avatarUrl: '/avatars/test.jpg' } } });
    const file = new File([''], 'test.jpg', { type: 'image/jpeg' });
    const res = await profileApi.uploadAvatar(file);
    expect(apiClient.post).toHaveBeenCalledTimes(1);
    const [url, formData] = (apiClient.post as any).mock.calls[0];
    expect(url).toBe('/profile/avatar');
    expect(formData).toBeInstanceOf(FormData);
    expect(formData.get('file')).toBe(file);
    expect(res.data.avatarUrl).toBe('/avatars/test.jpg');
  });
});
