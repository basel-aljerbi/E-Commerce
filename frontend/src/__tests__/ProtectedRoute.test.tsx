import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { ProtectedRoute } from '@/components/shared/protected-route';
import { useAuthStore } from '@/store/auth';

function TestComponent() {
  return <div>Protected Content</div>;
}

function LoginPage() {
  return <div>Login Page</div>;
}

describe('ProtectedRoute', () => {
  it('renders children when authenticated', () => {
    useAuthStore.setState({
      isAuthenticated: true,
      isAdmin: false,
      token: 'mock-token',
      user: {
        id: 1,
        email: 'test@test.com',
        fullName: 'Test User',
        role: 'User',
        isEmailVerified: true,
        createdAt: '',
      },
    });

    render(
      <MemoryRouter initialEntries={['/protected']}>
        <Routes>
          <Route
            path="/protected"
            element={
              <ProtectedRoute>
                <TestComponent />
              </ProtectedRoute>
            }
          />
          <Route path="/login" element={<LoginPage />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Protected Content')).toBeInTheDocument();
  });

  it('renders children for admin when admin required', () => {
    useAuthStore.setState({
      isAuthenticated: true,
      isAdmin: true,
      token: 'admin-token',
      user: {
        id: 2,
        email: 'admin@test.com',
        fullName: 'Admin User',
        role: 'Admin',
        isEmailVerified: true,
        createdAt: '',
      },
    });

    render(
      <MemoryRouter initialEntries={['/admin']}>
        <Routes>
          <Route
            path="/admin"
            element={
              <ProtectedRoute requireAdmin>
                <TestComponent />
              </ProtectedRoute>
            }
          />
          <Route path="/login" element={<LoginPage />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Protected Content')).toBeInTheDocument();
  });
});
