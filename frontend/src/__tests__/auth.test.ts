import { describe, it, expect } from 'vitest';
import { act } from '@testing-library/react';
import { useAuthStore } from '@/store/auth';

describe('Auth Store', () => {
  beforeEach(() => {
    useAuthStore.setState({
      token: null,
      refreshToken: null,
      user: null,
      isAuthenticated: false,
      isAdmin: false,
    });
    localStorage.clear();
  });

  it('sets auth state on login', () => {
    act(() => {
      useAuthStore.getState().setAuth({
        token: 'test-jwt',
        refreshToken: 'test-refresh',
        expiresAt: new Date(Date.now() + 3600000).toISOString(),
        email: 'test@test.com',
        role: 'User',
      });
    });

    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(true);
    expect(state.isAdmin).toBe(false);
    expect(state.token).toBe('test-jwt');
  });

  it('sets admin role correctly', () => {
    act(() => {
      useAuthStore.getState().setAuth({
        token: 'admin-jwt',
        refreshToken: 'admin-refresh',
        expiresAt: new Date(Date.now() + 3600000).toISOString(),
        email: 'admin@test.com',
        role: 'Admin',
      });
    });

    expect(useAuthStore.getState().isAdmin).toBe(true);
  });

  it('clears state on logout', () => {
    act(() => {
      useAuthStore.getState().setAuth({
        token: 'test-jwt',
        refreshToken: 'test-refresh',
        expiresAt: new Date(Date.now() + 3600000).toISOString(),
        email: 'test@test.com',
        role: 'User',
      });
    });

    act(() => {
      useAuthStore.getState().logout();
    });

    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(false);
    expect(state.token).toBeNull();
    expect(state.user).toBeNull();
  });
});
