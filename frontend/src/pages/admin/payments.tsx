import { useState } from 'react';
import { motion } from 'framer-motion';
import { CreditCard, ChevronLeft, ChevronRight } from 'lucide-react';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Button } from '@/components/ui/button';
import { formatPrice, formatDateTime } from '@/lib/utils';
import { useAdminPayments } from '@/hooks/useAdmin';

export default function AdminPaymentsPage() {
  const [page, setPage] = useState(1);
  const { data, isLoading } = useAdminPayments({ pageNumber: page, pageSize: 10 });
  const result = data?.data;

  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
    >
      <h1 className="text-3xl font-bold mb-8">Payments</h1>

      <Card>
        <CardHeader>
          <CardTitle>Payment Transactions</CardTitle>
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
                      <th className="pb-3 font-medium">ID</th>
                      <th className="pb-3 font-medium">Order</th>
                      <th className="pb-3 font-medium">Amount</th>
                      <th className="pb-3 font-medium">Status</th>
                      <th className="pb-3 font-medium">Date</th>
                    </tr>
                  </thead>
                  <tbody>
                    {result.items.map((payment) => (
                      <tr
                        key={payment.id}
                        className="border-b last:border-0 hover:bg-muted/50 transition-colors"
                      >
                        <td className="py-3 text-sm font-mono">#{payment.id}</td>
                        <td className="py-3 text-sm">#{payment.orderId}</td>
                        <td className="py-3 font-medium">
                          {formatPrice(payment.amount)}
                          <span className="text-xs text-muted-foreground ml-1 uppercase">{payment.currency}</span>
                        </td>
                        <td className="py-3">
                          <Badge
                            variant={
                              payment.status === 'succeeded' || payment.status === 'paid'
                                ? 'secondary'
                                : payment.status === 'failed'
                                  ? 'destructive'
                                  : 'default'
                            }
                          >
                            {payment.status}
                          </Badge>
                        </td>
                        <td className="py-3 text-sm text-muted-foreground">
                          {formatDateTime(payment.createdAt)}
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
            <div className="flex flex-col items-center justify-center py-12 text-muted-foreground">
              <CreditCard className="h-16 w-16 mb-4 opacity-20" />
              <p>No payment transactions yet</p>
            </div>
          )}
        </CardContent>
      </Card>
    </motion.div>
  );
}
