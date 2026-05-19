import { useState, useMemo } from 'react';
import { motion } from 'framer-motion';
import {
  DollarSign, ShoppingCart, Package, Users, TrendingUp, Star, AlertTriangle, CreditCard,
} from 'lucide-react';
import {
  LineChart, Line, BarChart, Bar, PieChart, Pie, AreaChart, Area,
  XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Cell, Legend,
} from 'recharts';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { formatPrice } from '@/lib/utils';
import { useAnalyticsFull, useMemoizedStats, useAnalyticsLowStock, useAnalyticsReviewStats, useOrderStatusBreakdown, usePaymentStats } from '@/hooks/useAnalytics';

const periods = [
  { value: 'day', label: 'Daily' },
  { value: 'month', label: 'Monthly' },
];

const CHART_COLORS = ['#10b981', '#3b82f6', '#8b5cf6', '#f59e0b', '#ef4444', '#06b6d4', '#ec4899'];
const PIE_COLORS = ['#10b981', '#3b82f6', '#f59e0b', '#ef4444', '#8b5cf6', '#06b6d4'];

function StatCard({ label, value, icon: Icon, color, format }: {
  label: string; value: number; icon: React.ElementType; color: string; format?: (v: number) => string;
}) {
  return (
    <Card className="hover:shadow-md transition-shadow">
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{label}</CardTitle>
        <div className={`p-2 rounded-lg bg-gradient-to-br ${color}`}>
          <Icon className="h-4 w-4 text-white" />
        </div>
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold tabular-nums">
          {format ? format(value) : value.toLocaleString()}
        </div>
        <p className="text-xs text-muted-foreground flex items-center gap-1 mt-1">
          <TrendingUp className="h-3 w-3 text-green-500" />
          Real-time
        </p>
      </CardContent>
    </Card>
  );
}

function OverviewCards({ overview }: { overview: any }) {
  const cards = [
    { label: 'Total Revenue', value: overview.totalRevenue, icon: DollarSign, color: 'from-emerald-500/20 to-emerald-500/5', format: formatPrice },
    { label: 'Total Orders', value: overview.totalOrders, icon: ShoppingCart, color: 'from-blue-500/20 to-blue-500/5' },
    { label: 'Total Products', value: overview.totalProducts, icon: Package, color: 'from-purple-500/20 to-purple-500/5' },
    { label: 'Total Users', value: overview.totalUsers, icon: Users, color: 'from-orange-500/20 to-orange-500/5' },
    { label: 'Low Stock', value: overview.lowStockProducts, icon: AlertTriangle, color: 'from-red-500/20 to-red-500/5' },
    { label: 'Pending Orders', value: overview.pendingOrders, icon: ShoppingCart, color: 'from-yellow-500/20 to-yellow-500/5' },
    { label: 'Avg Order Value', value: overview.averageOrderValue, icon: TrendingUp, color: 'from-cyan-500/20 to-cyan-500/5', format: formatPrice },
    { label: 'New Users Today', value: overview.newUsersToday, icon: Users, color: 'from-pink-500/20 to-pink-500/5' },
  ];

  return (
    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4 mb-8">
      {cards.map((card) => (
        <StatCard key={card.label} {...card} />
      ))}
    </div>
  );
}

function ChartCard({ title, children, className }: { title: string; children: React.ReactNode; className?: string }) {
  return (
    <Card className={className}>
      <CardHeader>
        <CardTitle className="text-base">{title}</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="h-72">{children}</div>
      </CardContent>
    </Card>
  );
}

function CustomTooltip({ active, payload, label }: any) {
  if (!active || !payload?.length) return null;
  return (
    <div className="rounded-lg border bg-background p-3 shadow-lg text-sm">
      <p className="font-medium mb-1">{label}</p>
      {payload.map((entry: any, i: number) => (
        <p key={i} style={{ color: entry.color }} className="tabular-nums">
          {entry.name}: {entry.name === 'Revenue' ? formatPrice(entry.value) : entry.value.toLocaleString()}
        </p>
      ))}
    </div>
  );
}

function RevenueChart({ data }: { data: Array<{ period: string; revenue: number }> }) {
  return (
    <ChartCard title="Revenue Trends">
      <ResponsiveContainer width="100%" height="100%">
        <AreaChart data={data}>
          <defs>
            <linearGradient id="revenueGrad" x1="0" y1="0" x2="0" y2="1">
              <stop offset="5%" stopColor="#10b981" stopOpacity={0.3} />
              <stop offset="95%" stopColor="#10b981" stopOpacity={0} />
            </linearGradient>
          </defs>
          <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" />
          <XAxis dataKey="period" tick={{ fontSize: 11 }} stroke="hsl(var(--muted-foreground))" />
          <YAxis tick={{ fontSize: 11 }} stroke="hsl(var(--muted-foreground))" tickFormatter={(v) => `$${(v / 1000).toFixed(0)}k`} />
          <Tooltip content={<CustomTooltip />} />
          <Area type="monotone" dataKey="revenue" name="Revenue" stroke="#10b981" fill="url(#revenueGrad)" strokeWidth={2} />
        </AreaChart>
      </ResponsiveContainer>
    </ChartCard>
  );
}

function OrdersChart({ data }: { data: Array<{ period: string; orderCount: number }> }) {
  return (
    <ChartCard title="Orders Over Time">
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={data}>
          <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" />
          <XAxis dataKey="period" tick={{ fontSize: 11 }} stroke="hsl(var(--muted-foreground))" />
          <YAxis tick={{ fontSize: 11 }} stroke="hsl(var(--muted-foreground))" />
          <Tooltip content={<CustomTooltip />} />
          <Bar dataKey="orderCount" name="Orders" fill="#3b82f6" radius={[4, 4, 0, 0]} />
        </BarChart>
      </ResponsiveContainer>
    </ChartCard>
  );
}

function UserGrowthChart({ data }: { data: Array<{ period: string; newUsers: number }> }) {
  return (
    <ChartCard title="User Registrations">
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={data}>
          <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" />
          <XAxis dataKey="period" tick={{ fontSize: 11 }} stroke="hsl(var(--muted-foreground))" />
          <YAxis tick={{ fontSize: 11 }} stroke="hsl(var(--muted-foreground))" />
          <Tooltip content={<CustomTooltip />} />
          <Line type="monotone" dataKey="newUsers" name="New Users" stroke="#8b5cf6" strokeWidth={2} dot={{ r: 3 }} />
        </LineChart>
      </ResponsiveContainer>
    </ChartCard>
  );
}

function TopProductsChart({ data }: { data: Array<{ productName: string; totalSold: number }> }) {
  const chartData = useMemo(() => data.slice(0, 10), [data]);
  return (
    <ChartCard title="Top Selling Products" className="lg:col-span-2">
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={chartData} layout="vertical">
          <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" />
          <XAxis type="number" tick={{ fontSize: 11 }} stroke="hsl(var(--muted-foreground))" />
          <YAxis dataKey="productName" type="category" tick={{ fontSize: 10 }} stroke="hsl(var(--muted-foreground))" width={140} />
          <Tooltip content={<CustomTooltip />} />
          <Bar dataKey="totalSold" name="Sold" fill="#f59e0b" radius={[0, 4, 4, 0]} />
        </BarChart>
      </ResponsiveContainer>
    </ChartCard>
  );
}

function OrderStatusPieChart({ data }: { data: Array<{ status: string; count: number }> }) {
  return (
    <ChartCard title="Order Status Distribution">
      <ResponsiveContainer width="100%" height="100%">
        <PieChart>
          <Pie data={data} dataKey="count" nameKey="status" cx="50%" cy="50%" outerRadius={80} label={({ status, percent }) => `${status} ${(percent * 100).toFixed(0)}%`}>
            {data.map((_, i) => (
              <Cell key={i} fill={PIE_COLORS[i % PIE_COLORS.length]} />
            ))}
          </Pie>
          <Tooltip />
        </PieChart>
      </ResponsiveContainer>
    </ChartCard>
  );
}

function PaymentPieChart({ data }: { data: Array<{ status: string; count: number }> }) {
  return (
    <ChartCard title="Payment Status">
      <ResponsiveContainer width="100%" height="100%">
        <PieChart>
          <Pie data={data} dataKey="count" nameKey="status" cx="50%" cy="50%" innerRadius={50} outerRadius={80}>
            {data.map((_, i) => (
              <Cell key={i} fill={PIE_COLORS[i % PIE_COLORS.length]} />
            ))}
          </Pie>
          <Tooltip />
          <Legend />
        </PieChart>
      </ResponsiveContainer>
    </ChartCard>
  );
}

function ReviewStatsCard({ reviewStats }: { reviewStats: any }) {
  if (!reviewStats) return null;
  const dist = Array.from({ length: 5 }, (_, i) => ({
    rating: i + 1,
    count: reviewStats.ratingDistribution?.[i + 1] || 0,
  })).reverse();

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base flex items-center gap-2">
          <Star className="h-4 w-4 text-yellow-500" />
          Reviews
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="text-center mb-4">
          <p className="text-3xl font-bold">{reviewStats.averageRating}</p>
          <p className="text-sm text-muted-foreground">avg rating ({reviewStats.totalReviews} reviews)</p>
        </div>
        <div className="space-y-1">
          {dist.map(({ rating, count }) => (
            <div key={rating} className="flex items-center gap-2 text-sm">
              <span className="w-8 text-right text-muted-foreground">{rating}★</span>
              <div className="flex-1 h-2 rounded-full bg-muted overflow-hidden">
                <div
                  className="h-full rounded-full bg-yellow-500 transition-all"
                  style={{ width: `${reviewStats.totalReviews > 0 ? (count / reviewStats.totalReviews) * 100 : 0}%` }}
                />
              </div>
              <span className="w-8 text-right text-muted-foreground tabular-nums">{count}</span>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}

function LowStockAlerts({ data }: { data: Array<{ productName: string; stockQuantity: number }> }) {
  if (!data?.length) return null;
  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base flex items-center gap-2">
          <AlertTriangle className="h-4 w-4 text-red-500" />
          Low Stock Alerts
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-2">
          {data.slice(0, 8).map((item) => (
            <div key={item.productName} className="flex items-center justify-between text-sm p-2 rounded-lg hover:bg-muted/50">
              <span className="truncate">{item.productName}</span>
              <Badge variant={item.stockQuantity <= 2 ? 'destructive' : 'warning'}>
                {item.stockQuantity} left
              </Badge>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}

export default function AdminAnalyticsPage() {
  const [period, setPeriod] = useState('month');
  const { data, isLoading, error } = useAnalyticsFull(period);
  const { data: lowStockData } = useAnalyticsLowStock();
  const { overview, revenueTrends, orderTrends, userGrowth, topProducts, paymentStats, orderStatusBreakdown, reviewStats } = useMemoizedStats(data);

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-48" />
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          {Array.from({ length: 8 }).map((_, i) => (
            <Skeleton key={i} className="h-28 rounded-xl" />
          ))}
        </div>
      </div>
    );
  }

  if (error || !overview) {
    return (
      <div className="text-center py-12 text-muted-foreground">
        Failed to load analytics data
      </div>
    );
  }

  const statsCards = [
    { label: 'Total Revenue', value: overview.totalRevenue, icon: DollarSign, color: 'from-emerald-500 to-emerald-600', format: formatPrice },
    { label: 'Total Orders', value: overview.totalOrders, icon: ShoppingCart, color: 'from-blue-500 to-blue-600' },
    { label: 'Total Products', value: overview.totalProducts, icon: Package, color: 'from-purple-500 to-purple-600' },
    { label: 'Total Users', value: overview.totalUsers, icon: Users, color: 'from-orange-500 to-orange-600' },
    { label: 'Avg Order Value', value: overview.averageOrderValue, icon: TrendingUp, color: 'from-cyan-500 to-cyan-600', format: formatPrice },
    { label: 'New Users Today', value: overview.newUsersToday, icon: Users, color: 'from-pink-500 to-pink-600' },
    { label: 'Pending Orders', value: overview.pendingOrders, icon: ShoppingCart, color: 'from-yellow-500 to-yellow-600' },
    { label: 'Total Reviews', value: overview.totalReviews, icon: Star, color: 'from-violet-500 to-violet-600' },
  ];

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }}>
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Analytics</h1>
          <p className="text-muted-foreground mt-1">Comprehensive business metrics and trends</p>
        </div>
        <div className="flex gap-2">
          {periods.map((p) => (
            <Button
              key={p.value}
              variant={period === p.value ? 'default' : 'outline'}
              size="sm"
              onClick={() => setPeriod(p.value)}
            >
              {p.label}
            </Button>
          ))}
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4 mb-8">
        {statsCards.map((card) => (
          <StatCard key={card.label} {...card} />
        ))}
      </div>

      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3 mb-8">
        <RevenueChart data={revenueTrends} />
        <OrdersChart data={orderTrends} />
        <UserGrowthChart data={userGrowth} />
      </div>

      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4 mb-8">
        <TopProductsChart data={topProducts} />
        <div className="space-y-6">
          <OrderStatusPieChart data={orderStatusBreakdown} />
          <LowStockAlerts data={lowStockData?.data ?? []} />
        </div>
        <div className="space-y-6">
          <PaymentPieChart data={paymentStats} />
          <ReviewStatsCard reviewStats={reviewStats} />
        </div>
      </div>
    </motion.div>
  );
}
