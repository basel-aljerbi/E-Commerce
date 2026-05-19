import { motion } from 'framer-motion';
import {
  DollarSign,
  ShoppingCart,
  Package,
  Users,
  TrendingUp,
} from 'lucide-react';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { Badge } from '@/components/ui/badge';
import { formatPrice, formatDate } from '@/lib/utils';
import { useAdminDashboard } from '@/hooks/useAdmin';
import { OrderStatusLabel, type OrderStatus } from '@/types/order';
import { getOrderStatusColor } from '@/utils/order';

const statCards = [
  { label: 'Total Revenue', key: 'totalRevenue' as const, icon: DollarSign, color: 'text-green-500', format: (v: number) => formatPrice(v), bg: 'from-green-500/20 to-green-500/5' },
  { label: 'Total Orders', key: 'totalOrders' as const, icon: ShoppingCart, color: 'text-blue-500', format: (v: number) => v.toLocaleString(), bg: 'from-blue-500/20 to-blue-500/5' },
  { label: 'Total Products', key: 'totalProducts' as const, icon: Package, color: 'text-purple-500', format: (v: number) => v.toLocaleString(), bg: 'from-purple-500/20 to-purple-500/5' },
  { label: 'Total Users', key: 'totalUsers' as const, icon: Users, color: 'text-orange-500', format: (v: number) => v.toLocaleString(), bg: 'from-orange-500/20 to-orange-500/5' },
];

export default function AdminDashboardPage() {
  const { data, isLoading, error } = useAdminDashboard();
  const dashboard = data?.data;

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-48" />
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-32 rounded-xl" />
          ))}
        </div>
      </div>
    );
  }

  if (error || !dashboard) {
    return (
      <div className="text-center py-12 text-muted-foreground">
        Failed to load dashboard data
      </div>
    );
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
    >
      <h1 className="text-3xl font-bold mb-8 tracking-tight">Dashboard</h1>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4 mb-8">
        {statCards.map((stat) => {
          const Icon = stat.icon;
          return (
            <Card key={stat.label} className="hover:shadow-md transition-shadow">
              <CardHeader className="flex flex-row items-center justify-between pb-2">
                <CardTitle className="text-sm font-medium text-muted-foreground">
                  {stat.label}
                </CardTitle>
                <div className={`p-2 rounded-lg bg-gradient-to-br ${stat.bg}`}>
                  <Icon className={`h-4 w-4 ${stat.color}`} />
                </div>
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold tabular-nums">
                  {stat.format(dashboard[stat.key])}
                </div>
                <p className="text-xs text-muted-foreground flex items-center gap-1 mt-1">
                  <TrendingUp className="h-3 w-3 text-green-500" />
                  Live
                </p>
              </CardContent>
            </Card>
          );
        })}
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-7">
        <Card className="lg:col-span-4 hover:shadow-md transition-shadow">
          <CardHeader>
            <CardTitle>Revenue Overview</CardTitle>
          </CardHeader>
          <CardContent>
            {dashboard.revenueByMonth.length > 0 ? (
              <div className="space-y-2">
                {dashboard.revenueByMonth.map((r) => (
                  <div key={r.month} className="flex items-center justify-between text-sm p-2 rounded-lg hover:bg-muted/50 transition-colors">
                    <span className="text-muted-foreground">{r.month}</span>
                    <span className="font-medium tabular-nums">{formatPrice(r.revenue)}</span>
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">No revenue data yet</p>
            )}
          </CardContent>
        </Card>

        <Card className="lg:col-span-3 hover:shadow-md transition-shadow">
          <CardHeader>
            <CardTitle>Recent Orders</CardTitle>
          </CardHeader>
          <CardContent>
            {dashboard.recentOrders.length > 0 ? (
              <div className="space-y-3">
                {dashboard.recentOrders.slice(0, 5).map((order) => (
                  <div key={order.id} className="flex items-center justify-between text-sm p-2 rounded-lg hover:bg-muted/50 transition-colors">
                    <div>
                      <p className="font-medium">#{order.id}</p>
                      <p className="text-muted-foreground">{formatDate(order.orderDate)}</p>
                    </div>
                    <div className="text-right">
                      <p className="font-medium tabular-nums">{formatPrice(order.totalAmount)}</p>
                      <Badge variant={getOrderStatusColor(order.status as OrderStatus)}>
                        {OrderStatusLabel[order.status as OrderStatus]}
                      </Badge>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">No orders yet</p>
            )}
          </CardContent>
        </Card>
      </div>
    </motion.div>
  );
}
