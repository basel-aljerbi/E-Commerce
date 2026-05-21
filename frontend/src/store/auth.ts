import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { AuthResponseDto, User } from '@/types/auth';
import { jwtDecode } from '@/utils/jwt';
import { useCartStore } from '@/store/cart';

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
        const decoded = jwtDecode(response.token, response.email, response.role);
        set({
          token: response.token,
          refreshToken: response.refreshToken,
          user: decoded as User | null,
          isAuthenticated: true,
          isAdmin: response.role === 'Admin',
        });
      },

      setTokens: (token: string, refreshToken: string) => {
        const decoded = jwtDecode(token);
        set({
          token,
          refreshToken,
          user: decoded as User | null,
          isAuthenticated: true,
          isAdmin: decoded?.role === 'Admin',
        });
      },

      logout: () => {
        useCartStore.getState().clearCart();
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
      onRehydrateStorage: () => (state, error) => {
        if (!error && state?.token && state?.user?.id === 0) {
          const decoded = jwtDecode(state.token, state.user.email, state.user.role);
          if (decoded && decoded.id > 0) {
            useAuthStore.setState({ user: decoded as User });
          }
        }
      },
    }
  )
);
