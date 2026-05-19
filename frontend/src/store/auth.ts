import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { AuthResponseDto, User } from '@/types/auth';
import { jwtDecode } from '@/utils/jwt';

interface AuthState {
  token: string | null;
  refreshToken: string | null;
  user: User | null;
  isAuthenticated: boolean;
  isAdmin: boolean;
  setAuth: (response: AuthResponseDto) => void;
  setTokens: (token: string, refreshToken: string) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      refreshToken: null,
      user: null,
      isAuthenticated: false,
      isAdmin: false,

      setAuth: (response: AuthResponseDto) => {
        const user = jwtDecode(response.token, response.email, response.role);
        set({
          token: response.token,
          refreshToken: response.refreshToken,
          user,
          isAuthenticated: true,
          isAdmin: response.role === 'Admin',
        });
      },

      setTokens: (token: string, refreshToken: string) => {
        const user = jwtDecode(token);
        set({
          token,
          refreshToken,
          user,
          isAuthenticated: true,
          isAdmin: user?.role === 'Admin' ?? false,
        });
      },

      logout: () => {
        set({
          token: null,
          refreshToken: null,
          user: null,
          isAuthenticated: false,
          isAdmin: false,
        });
      },
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        token: state.token,
        refreshToken: state.refreshToken,
        user: state.user,
        isAuthenticated: state.isAuthenticated,
        isAdmin: state.isAdmin,
      }),
    }
  )
);
