import { useSearchParams, Link } from 'react-router-dom';
import { CheckCircle, Package, ArrowLeft } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { useOrder } from '@/hooks/useOrders';
import { formatPrice } from '@/lib/utils';

export default function OrderSuccessPage() {
  const [searchParams] = useSearchParams();
  const orderId = Number(searchParams.get('orderId')) || 0;
  const { data } = useOrder(orderId);
  const order = data?.data;

  return (
    <div className="mx-auto max-w-2xl px-4 sm:px-6 lg:px-8 py-20">
      <div className="text-center">
        <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-green-100 dark:bg-green-900/30 mb-6">
          <CheckCircle className="h-8 w-8 text-green-600 dark:text-green-400" />
        </div>
        <h1 className="text-3xl font-bold mb-2">Order Placed!</h1>
        <p className="text-muted-foreground mb-8">
          Thank you for your purchase. Your order has been placed successfully.
        </p>

        {order && (
          <div className="rounded-lg border p-6 mb-8 text-left">
            <div className="flex items-center justify-between mb-4">
              <span className="text-sm text-muted-foreground">
                Order #{order.id}
              </span>
              <span className="font-semibold">
                {formatPrice(order.totalAmount)}
              </span>
            </div>
            <div className="space-y-3">
              {order.items.map((item) => (
                <div
                  key={item.id}
                  className="flex items-center justify-between text-sm"
                >
                  <span>
                    {item.productName} x{item.quantity}
                  </span>
                  <span>{formatPrice(item.totalPrice)}</span>
                </div>
              ))}
            </div>
          </div>
        )}

        <div className="flex items-center justify-center gap-4">
          <Link to="/account/orders">
            <Button>
              <Package className="h-4 w-4 mr-2" />
              View Orders
            </Button>
          </Link>
          <Link to="/products">
            <Button variant="outline">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Continue Shopping
            </Button>
          </Link>
        </div>
      </div>
    </div>
  );
}
