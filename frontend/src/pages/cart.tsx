import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import {
  ShoppingBag,
  Plus,
  Minus,
  Trash2,
  ArrowLeft,
  Shield,
  Truck,
  RefreshCw,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { formatPrice, getImageUrl } from '@/lib/utils';
import { useCartStore } from '@/store/cart';
import { useCart } from '@/hooks/useCart';

export default function CartPage() {
  useCart();
  const { items, updateQuantity, removeItem, totalAmount, itemCount } =
    useCartStore();

  if (items.length === 0) {
    return (
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-20">
        <div className="flex flex-col items-center justify-center text-center">
          <div className="mb-6 p-6 rounded-full bg-muted">
            <ShoppingBag className="h-16 w-16 text-muted-foreground/30" />
          </div>
          <h1 className="text-3xl font-bold mb-2">Your cart is empty</h1>
          <p className="text-muted-foreground mb-8">
            Looks like you have not added any items yet.
          </p>
          <Link to="/products">
            <Button size="lg" className="gap-2">
              <ArrowLeft className="h-4 w-4" />
              Continue Shopping
            </Button>
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-8">
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Shopping Cart</h1>
          <p className="text-muted-foreground mt-1">
            {itemCount()} item{itemCount() !== 1 ? 's' : ''} in your cart
          </p>
        </div>
        <Link to="/products" className="hidden sm:block">
          <Button variant="outline" className="gap-2">
            <ArrowLeft className="h-4 w-4" />
            Continue Shopping
          </Button>
        </Link>
      </div>

      <div className="grid lg:grid-cols-3 gap-8">
        <div className="lg:col-span-2 space-y-4">
          {items.map((item) => (
            <motion.div
              key={item.productId}
              layout
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              className="flex gap-4 rounded-xl border bg-card p-4 shadow-sm hover:shadow-md transition-shadow"
            >
              <div className="h-24 w-24 shrink-0 overflow-hidden rounded-lg bg-muted">
                {item.imageUrl ? (
                  <img
                    src={getImageUrl(item.imageUrl)}
                    alt={item.productName}
                    className="h-full w-full object-cover"
                  />
                ) : (
                  <div className="flex h-full items-center justify-center">
                    <ShoppingBag className="h-8 w-8 text-muted-foreground/30" />
                  </div>
                )}
              </div>
              <div className="flex flex-1 flex-col justify-between">
                <div>
                  <Link
                    to={`/products/${item.productId}`}
                    className="font-medium hover:text-primary transition-colors"
                  >
                    {item.productName}
                  </Link>
                  <p className="text-sm text-muted-foreground mt-1">
                    {formatPrice(item.unitPrice)} each
                  </p>
                </div>
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <Button
                      variant="outline"
                      size="icon"
                      className="h-8 w-8 shadow-sm"
                      onClick={() =>
                        updateQuantity(item.productId, item.quantity - 1)
                      }
                    >
                      <Minus className="h-3 w-3" />
                    </Button>
                    <span className="w-8 text-center font-medium tabular-nums">
                      {item.quantity}
                    </span>
                    <Button
                      variant="outline"
                      size="icon"
                      className="h-8 w-8 shadow-sm"
                      onClick={() =>
                        updateQuantity(item.productId, item.quantity + 1)
                      }
                    >
                      <Plus className="h-3 w-3" />
                    </Button>
                  </div>
                  <div className="flex items-center gap-4">
                    <span className="font-semibold tabular-nums">
                      {formatPrice(item.unitPrice * item.quantity)}
                    </span>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8 text-destructive hover:bg-destructive/10"
                      onClick={() => removeItem(item.productId)}
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </div>
            </motion.div>
          ))}
        </div>

        <div className="lg:col-span-1">
          <div className="rounded-xl border bg-gradient-to-br from-card to-muted/30 backdrop-blur-sm p-6 space-y-4 shadow-sm">
            <h2 className="text-lg font-semibold">Order Summary</h2>
            <div className="space-y-2">
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Subtotal</span>
                <span className="tabular-nums">{formatPrice(totalAmount())}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Shipping</span>
                <span className="text-green-600 font-medium">Free</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Tax</span>
                <span>Calculated at checkout</span>
              </div>
            </div>
            <Separator />
            <div className="flex justify-between font-semibold text-lg">
              <span>Total</span>
              <span className="tabular-nums">{formatPrice(totalAmount())}</span>
            </div>
            <Link to="/checkout">
              <Button className="w-full shadow-lg" size="lg">
                Proceed to Checkout
              </Button>
            </Link>
            <div className="grid grid-cols-3 gap-2 pt-2">
              <div className="flex flex-col items-center gap-1 text-[10px] text-muted-foreground">
                <Truck className="h-4 w-4" />
                Free ship
              </div>
              <div className="flex flex-col items-center gap-1 text-[10px] text-muted-foreground">
                <Shield className="h-4 w-4" />
                Secure
              </div>
              <div className="flex flex-col items-center gap-1 text-[10px] text-muted-foreground">
                <RefreshCw className="h-4 w-4" />
                Returns
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
