import { lazy, Suspense } from 'react';
import { createBrowserRouter, Navigate } from 'react-router-dom';
import { RootLayout } from '@/components/layout/root-layout';
import { AdminLayout } from '@/components/layout/admin-layout';
import { ProtectedRoute } from '@/components/shared/protected-route';
import { Skeleton } from '@/components/ui/skeleton';

const HomePage = lazy(() => import('@/pages/home'));
const ProductsPage = lazy(() => import('@/pages/products'));
const ProductDetailPage = lazy(() => import('@/pages/product-detail'));
const CartPage = lazy(() => import('@/pages/cart'));
const CheckoutPage = lazy(() => import('@/pages/checkout'));
const OrderSuccessPage = lazy(() => import('@/pages/order-success'));
const WishlistPage = lazy(() => import('@/pages/wishlist'));
const LoginPage = lazy(() => import('@/pages/login'));
const RegisterPage = lazy(() => import('@/pages/register'));
const ForgotPasswordPage = lazy(() => import('@/pages/forgot-password'));
const ResetPasswordPage = lazy(() => import('@/pages/reset-password'));
const VerifyEmailPage = lazy(() => import('@/pages/verify-email'));

const ProfilePage = lazy(() => import('@/pages/account/profile'));
const OrdersPage = lazy(() => import('@/pages/account/orders'));
const OrderDetailPage = lazy(() => import('@/pages/account/order-detail'));
const AccountWishlistPage = lazy(() => import('@/pages/wishlist'));

const AdminDashboardPage = lazy(() => import('@/pages/admin/dashboard'));
const AdminProductsPage = lazy(() => import('@/pages/admin/products'));
const AdminOrdersPage = lazy(() => import('@/pages/admin/orders'));
const AdminUsersPage = lazy(() => import('@/pages/admin/users'));
const AdminAuditLogsPage = lazy(() => import('@/pages/admin/audit-logs'));
const AdminAnalyticsPage = lazy(() => import('@/pages/admin/analytics'));
const AdminPaymentsPage = lazy(() => import('@/pages/admin/payments'));
const AdminCategoriesPage = lazy(() => import('@/pages/admin/categories'));

function PageLoader() {
  return (
    <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-8 space-y-4">
      <Skeleton className="h-8 w-48" />
      <div className="grid grid-cols-4 gap-6">
        {Array.from({ length: 4 }).map((_, i) => (
          <Skeleton key={i} className="aspect-square rounded-xl" />
        ))}
      </div>
    </div>
  );
}

function SuspenseWrapper({ children }: { children: React.ReactNode }) {
  return <Suspense fallback={<PageLoader />}>{children}</Suspense>;
}

export const router = createBrowserRouter([
  {
    path: '/',
    element: <RootLayout />,
    children: [
      { index: true, element: <SuspenseWrapper><HomePage /></SuspenseWrapper> },
      { path: 'products', element: <SuspenseWrapper><ProductsPage /></SuspenseWrapper> },
      { path: 'products/:id', element: <SuspenseWrapper><ProductDetailPage /></SuspenseWrapper> },
      { path: 'cart', element: <SuspenseWrapper><CartPage /></SuspenseWrapper> },
      {
        path: 'checkout',
        element: (
          <ProtectedRoute>
            <SuspenseWrapper><CheckoutPage /></SuspenseWrapper>
          </ProtectedRoute>
        ),
      },
      { path: 'order/success', element: <SuspenseWrapper><OrderSuccessPage /></SuspenseWrapper> },
      { path: 'wishlist', element: <SuspenseWrapper><WishlistPage /></SuspenseWrapper> },
      { path: 'login', element: <SuspenseWrapper><LoginPage /></SuspenseWrapper> },
      { path: 'register', element: <SuspenseWrapper><RegisterPage /></SuspenseWrapper> },
      { path: 'forgot-password', element: <SuspenseWrapper><ForgotPasswordPage /></SuspenseWrapper> },
      { path: 'reset-password', element: <SuspenseWrapper><ResetPasswordPage /></SuspenseWrapper> },
      { path: 'verify-email', element: <SuspenseWrapper><VerifyEmailPage /></SuspenseWrapper> },

      {
        path: 'account',
        element: (
          <ProtectedRoute>
            <SuspenseWrapper><ProfilePage /></SuspenseWrapper>
          </ProtectedRoute>
        ),
      },
      {
        path: 'account/orders',
        element: (
          <ProtectedRoute>
            <SuspenseWrapper><OrdersPage /></SuspenseWrapper>
          </ProtectedRoute>
        ),
      },
      {
        path: 'account/orders/:id',
        element: (
          <ProtectedRoute>
            <SuspenseWrapper><OrderDetailPage /></SuspenseWrapper>
          </ProtectedRoute>
        ),
      },
      {
        path: 'account/wishlist',
        element: (
          <ProtectedRoute>
            <SuspenseWrapper><AccountWishlistPage /></SuspenseWrapper>
          </ProtectedRoute>
        ),
      },
    ],
  },
  {
    path: '/admin',
    element: (
      <ProtectedRoute requireAdmin>
        <AdminLayout />
      </ProtectedRoute>
    ),
    children: [
      { index: true, element: <SuspenseWrapper><AdminDashboardPage /></SuspenseWrapper> },
      { path: 'products', element: <SuspenseWrapper><AdminProductsPage /></SuspenseWrapper> },
      { path: 'orders', element: <SuspenseWrapper><AdminOrdersPage /></SuspenseWrapper> },
      { path: 'users', element: <SuspenseWrapper><AdminUsersPage /></SuspenseWrapper> },
      { path: 'audit-logs', element: <SuspenseWrapper><AdminAuditLogsPage /></SuspenseWrapper> },
      { path: 'analytics', element: <SuspenseWrapper><AdminAnalyticsPage /></SuspenseWrapper> },
      { path: 'payments', element: <SuspenseWrapper><AdminPaymentsPage /></SuspenseWrapper> },
      { path: 'categories', element: <SuspenseWrapper><AdminCategoriesPage /></SuspenseWrapper> },
    ],
  },

  { path: '*', element: <Navigate to="/" replace /> },
]);
