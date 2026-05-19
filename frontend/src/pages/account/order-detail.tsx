import { useParams, Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { ArrowLeft, Package } from 'lucide-react';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { Skeleton } from '@/components/ui/skeleton';
import { useOrder } from '@/hooks/useOrders';
import { useCancelOrder } from '@/hooks/useOrders';
import { OrderStatus, OrderStatusLabel } from '@/types/order';
import { formatPrice, formatDate, formatDateTime } from '@/lib/utils';
import { getOrderStatusColor } from '@/utils/order';

export default function OrderDetailPage() {
  const { id } = useParams<{ id: string }>();
  const orderId = Number(id);
  const { data, isLoading } = useOrder(orderId);
  const cancelOrder = useCancelOrder();
  const order = data?.data;

  if (isLoading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-64 rounded-xl" />
      </div>
    );
  }

  if (!order) {
    return (
      <div className="text-center py-20">
        <Package className="h-16 w-16 mx-auto text-muted-foreground/30 mb-4" />
        <h2 className="text-xl font-semibold">Order not found</h2>
        <Link to="/account/orders">
          <Button className="mt-4">Back to Orders</Button>
        </Link>
      </div>
    );
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
    >
      <Link
        to="/account/orders"
        className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground mb-6"
      >
        <ArrowLeft className="h-4 w-4" />
        Back to Orders
      </Link>

      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-3xl font-bold">Order #{order.id}</h1>
          <p className="text-muted-foreground mt-1">
            {formatDate(order.orderDate)}
          </p>
        </div>
        <Badge variant={getOrderStatusColor(order.status)} className="text-sm px-3 py-1">
          {OrderStatusLabel[order.status as OrderStatus]}
        </Badge>
      </div>

      <Card className="mb-6">
        <CardHeader>
          <CardTitle>Items</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {order.items.map((item) => (
              <div
                key={item.id}
                className="flex items-center justify-between"
              >
                <div>
                  <Link
                    to={`/products/${item.productId}`}
                    className="font-medium hover:text-primary transition-colors"
                  >
                    {item.productName}
                  </Link>
                  <p className="text-sm text-muted-foreground">
                    Qty: {item.quantity} x {formatPrice(item.unitPrice)}
                  </p>
                </div>
                <p className="font-semibold">
                  {formatPrice(item.totalPrice)}
                </p>
              </div>
            ))}
          </div>
          <Separator className="my-4" />
          <div className="flex justify-between font-semibold text-lg">
            <span>Total</span>
            <span>{formatPrice(order.totalAmount)}</span>
          </div>
        </CardContent>
      </Card>

      {order.statusHistory && order.statusHistory.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Order History</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {order.statusHistory.map((history) => (
                <div key={history.id} className="flex items-start gap-4">
                  <div className="mt-1 h-2 w-2 rounded-full bg-primary shrink-0" />
                  <div>
                    <div className="flex items-center gap-2">
                      <Badge variant="secondary" className="text-xs">
                        {OrderStatusLabel[history.fromStatus as OrderStatus]}
                      </Badge>
                      <span className="text-xs text-muted-foreground">→</span>
                      <Badge variant="secondary" className="text-xs">
                        {OrderStatusLabel[history.toStatus as OrderStatus]}
                      </Badge>
                    </div>
                    {history.reason && (
                      <p className="text-sm text-muted-foreground mt-1">
                        {history.reason}
                      </p>
                    )}
                    <p className="text-xs text-muted-foreground mt-1">
                      {formatDateTime(history.changedAt)}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {(order.status === OrderStatus.Pending ||
        order.status === OrderStatus.PaymentProcessing) && (
        <div className="mt-6">
          <Button
            variant="destructive"
            onClick={() => cancelOrder.mutate(order.id)}
            disabled={cancelOrder.isPending}
          >
            Cancel Order
          </Button>
        </div>
      )}
    </motion.div>
  );
}
