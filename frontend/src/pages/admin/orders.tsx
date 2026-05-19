import { useState } from 'react';
import { motion } from 'framer-motion';
import { Search, ChevronLeft, ChevronRight } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Select } from '@/components/ui/select';
import { useAdminOrders, useAdminUpdateOrderStatus } from '@/hooks/useAdmin';
import { OrderStatus, OrderStatusLabel, type OrderStatus as OrderStatusType } from '@/types/order';
import { formatPrice, formatDate } from '@/lib/utils';
import { getOrderStatusColor } from '@/utils/order';

const statusFilterOptions = [
  { value: '', label: 'All statuses' },
  ...Object.entries(OrderStatusLabel)
    .filter(([k]) => !isNaN(Number(k)))
    .map(([k, v]) => ({ value: v, label: v })),
];

export default function AdminOrdersPage() {
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState('');

  const { data, isLoading } = useAdminOrders({
    pageNumber: page,
    pageSize: 10,
    status: statusFilter || undefined,
  });
  const result = data?.data;
  const updateStatus = useAdminUpdateOrderStatus();

  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
    >
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-3xl font-bold tracking-tight">Orders</h1>
        <Select
          value={statusFilter}
          onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
          options={statusFilterOptions}
          className="w-44"
        />
      </div>

      <Card className="hover:shadow-md transition-shadow">
        <CardHeader>
          <CardTitle>All Orders</CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-3">
              {Array.from({ length: 5 }).map((_, i) => (
                <Skeleton key={i} className="h-16 rounded-lg" />
              ))}
            </div>
          ) : result && result.items.length > 0 ? (
            <>
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b text-left text-sm text-muted-foreground">
                      <th className="pb-3 font-medium">Order ID</th>
                      <th className="pb-3 font-medium">Date</th>
                      <th className="pb-3 font-medium">Items</th>
                      <th className="pb-3 font-medium">Total</th>
                      <th className="pb-3 font-medium">Status</th>
                      <th className="pb-3 font-medium">Update</th>
                    </tr>
                  </thead>
                  <tbody>
                    {result.items.map((order) => (
                      <tr
                        key={order.id}
                        className="border-b last:border-0 hover:bg-muted/50 transition-colors"
                      >
                        <td className="py-3 font-medium">#{order.id}</td>
                        <td className="py-3 text-sm text-muted-foreground">
                          {formatDate(order.orderDate)}
                        </td>
                        <td className="py-3 text-sm">
                          {order.items.length} item{order.items.length !== 1 ? 's' : ''}
                        </td>
                        <td className="py-3 font-medium tabular-nums">
                          {formatPrice(order.totalAmount)}
                        </td>
                        <td className="py-3">
                          <Badge variant={getOrderStatusColor(order.status as OrderStatusType)} className="relative pl-6">
                            <span className="absolute left-1.5 top-1/2 -translate-y-1/2 w-1.5 h-1.5 rounded-full bg-current opacity-60" />
                            {OrderStatusLabel[order.status as OrderStatusType]}
                          </Badge>
                        </td>
                        <td className="py-3">
                          <Select
                            value=""
                            onChange={(e) => {
                              if (e.target.value) {
                                updateStatus.mutate({ id: order.id, status: e.target.value });
                              }
                            }}
                            options={[
                              { value: '', label: 'Change to...' },
                              ...Object.entries(OrderStatusLabel)
                                .filter(([k]) => !isNaN(Number(k)))
                                .filter(([k]) => Number(k) !== order.status)
                                .map(([k, v]) => ({ value: v, label: v })),
                            ]}
                            className="w-36 h-8 text-xs"
                          />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {result.totalPages > 1 && (
                <div className="flex items-center justify-between mt-4 pt-4 border-t">
                  <p className="text-sm text-muted-foreground">
                    Page {result.pageNumber} of {result.totalPages} ({result.totalCount} total)
                  </p>
                  <div className="flex items-center gap-2">
                    <Button
                      variant="outline"
                      size="icon"
                      disabled={!result.hasPreviousPage}
                      onClick={() => setPage((p) => p - 1)}
                    >
                      <ChevronLeft className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="outline"
                      size="icon"
                      disabled={!result.hasNextPage}
                      onClick={() => setPage((p) => p + 1)}
                    >
                      <ChevronRight className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              )}
            </>
          ) : (
            <p className="text-sm text-muted-foreground py-8 text-center">No orders found</p>
          )}
        </CardContent>
      </Card>
    </motion.div>
  );
}
