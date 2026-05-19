import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { ShoppingCart, Heart } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { formatPrice, getImageUrl } from '@/lib/utils';
import type { ProductDto } from '@/types';
import { useCartStore } from '@/store/cart';
import { useAuthStore } from '@/store/auth';
import { useAddToCart } from '@/hooks/useCart';

interface ProductCardProps {
  product: ProductDto;
  index?: number;
  inWishlist?: boolean;
  onToggleWishlist?: (productId: number) => void;
}

export function ProductCard({
  product,
  index = 0,
  inWishlist,
  onToggleWishlist,
}: ProductCardProps) {
  const addItem = useCartStore((s) => s.addItem);
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const addToCartApi = useAddToCart();

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.3, delay: index * 0.05 }}
    >
      <Card className="group overflow-hidden border-0 shadow-sm hover:shadow-xl transition-all duration-300 hover:-translate-y-1">
        <Link to={`/products/${product.id}`}>
          <div className="relative aspect-square overflow-hidden bg-muted">
            {product.imageUrl ? (
              <img
                src={getImageUrl(product.imageUrl)}
                alt={product.name}
                className="h-full w-full object-cover transition-transform duration-700 group-hover:scale-110"
              />
            ) : (
              <div className="flex h-full items-center justify-center text-muted-foreground">
                <ShoppingCart className="h-12 w-12 opacity-20" />
              </div>
            )}
            {product.stockQuantity <= 0 && (
              <Badge
                variant="destructive"
                className="absolute top-3 left-3"
              >
                Out of Stock
              </Badge>
            )}
            {product.stockQuantity > 0 && product.stockQuantity <= 5 && (
              <Badge
                variant="warning"
                className="absolute top-3 left-3"
              >
                Only {product.stockQuantity} left
              </Badge>
            )}
            <div className="absolute inset-x-0 bottom-0 h-20 bg-gradient-to-t from-black/20 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300" />
          </div>
        </Link>

        <CardContent className="p-4">
          <div className="mb-1">
            <span className="text-xs text-muted-foreground">
              {product.categoryName}
            </span>
          </div>
          <Link
            to={`/products/${product.id}`}
            className="block"
          >
            <h3 className="font-medium leading-tight hover:text-primary transition-colors line-clamp-1">
              {product.name}
            </h3>
          </Link>
          <p className="mt-1 font-semibold tabular-nums">{formatPrice(product.price)}</p>

          <div className="mt-3 flex items-center gap-2 translate-y-2 opacity-0 group-hover:translate-y-0 group-hover:opacity-100 transition-all duration-300">
            <Button
              size="sm"
              className="flex-1 shadow-sm"
              disabled={product.stockQuantity <= 0}
              onClick={(e) => {
                e.preventDefault();
                addItem({
                  productId: product.id,
                  productName: product.name,
                  unitPrice: product.price,
                  quantity: 1,
                  imageUrl: product.imageUrl,
                });
                if (isAuthenticated) {
                  addToCartApi.mutate({ productId: product.id, quantity: 1 });
                }
              }}
            >
              <ShoppingCart className="h-4 w-4 mr-1" />
              Add to Cart
            </Button>
            {onToggleWishlist && (
              <Button
                variant="outline"
                size="icon"
                className="shadow-sm"
                onClick={(e) => {
                  e.preventDefault();
                  onToggleWishlist(product.id);
                }}
              >
                <Heart
                  className={`h-4 w-4 ${
                    inWishlist ? 'fill-red-500 text-red-500' : ''
                  }`}
                />
              </Button>
            )}
          </div>
        </CardContent>
      </Card>
    </motion.div>
  );
}
