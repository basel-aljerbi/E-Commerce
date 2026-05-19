import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import {
  ShoppingCart,
  Heart,
  Minus,
  Plus,
  ChevronLeft,
  Package,
  Truck,
  Shield,
  Star,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Separator } from '@/components/ui/separator';
import { StarRating } from '@/components/shared/star-rating';
import { RelatedProducts } from '@/components/shared/related-products';
import { useProduct } from '@/hooks/useProducts';
import { useReviews } from '@/hooks/useReviews';
import { useAddToCart } from '@/hooks/useCart';
import { useAddToWishlist, useRemoveFromWishlist, useWishlist } from '@/hooks/useWishlist';
import { useCartStore } from '@/store/cart';
import { useAuthStore } from '@/store/auth';
import { formatPrice, getImageUrl } from '@/lib/utils';

export default function ProductDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const productId = Number(id);
  const { data: productData, isLoading } = useProduct(productId);
  const { data: reviewsData } = useReviews(productId);
  const { data: wishlistData } = useWishlist();
  const addToCartHook = useAddToCart();
  const addToWishlistHook = useAddToWishlist();
  const removeFromWishlistHook = useRemoveFromWishlist();
  const { isAuthenticated } = useAuthStore();
  const addItemToCart = useCartStore((s) => s.addItem);

  const [quantity, setQuantity] = useState(1);
  const product = productData?.data;
  const reviews = reviewsData?.data;
  const wishlistIds = new Set(
    wishlistData?.data?.map((w) => w.productId) ?? []
  );
  const inWishlist = wishlistIds.has(productId);

  const handleAddToCart = () => {
    if (!product) return;
    addItemToCart({
      productId: product.id,
      productName: product.name,
      unitPrice: product.price,
      quantity,
      imageUrl: product.imageUrl,
    });
    if (isAuthenticated) {
      addToCartHook.mutate({ productId: product.id, quantity });
    }
  };

  const handleToggleWishlist = () => {
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }
    if (inWishlist) {
      removeFromWishlistHook.mutate(productId);
    } else {
      addToWishlistHook.mutate(productId);
    }
  };

  if (isLoading) {
    return (
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-8">
        <div className="grid md:grid-cols-2 gap-12">
          <Skeleton className="aspect-square rounded-2xl" />
          <div className="space-y-4">
            <Skeleton className="h-8 w-3/4" />
            <Skeleton className="h-6 w-1/4" />
            <Skeleton className="h-24 w-full" />
            <Skeleton className="h-10 w-1/3" />
          </div>
        </div>
      </div>
    );
  }

  if (!product) {
    return (
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-20 text-center">
        <h2 className="text-2xl font-bold">Product not found</h2>
        <p className="text-muted-foreground mt-2">
          The product you are looking for does not exist.
        </p>
        <Button className="mt-6" onClick={() => navigate('/products')}>
          <ChevronLeft className="h-4 w-4 mr-1" />
          Back to Products
        </Button>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-8">
      <motion.button
        initial={{ opacity: 0, x: -10 }}
        animate={{ opacity: 1, x: 0 }}
        onClick={() => navigate(-1)}
        className="flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors mb-6 group"
      >
        <ChevronLeft className="h-4 w-4 transition-transform group-hover:-translate-x-0.5" />
        Back
      </motion.button>

      <div className="grid md:grid-cols-2 gap-12">
        <motion.div
          initial={{ opacity: 0, x: -20 }}
          animate={{ opacity: 1, x: 0 }}
          className="relative"
        >
          <div className="absolute -inset-4 bg-gradient-to-br from-primary/5 to-primary/10 rounded-[2.5rem] backdrop-blur-sm" />
          <div className="relative aspect-square overflow-hidden rounded-2xl bg-gradient-to-br from-muted to-muted/50 shadow-2xl shadow-primary/10">
            {product.imageUrl ? (
              <img
                src={getImageUrl(product.imageUrl)}
                alt={product.name}
                className="h-full w-full object-cover transition-transform duration-700 hover:scale-105"
              />
            ) : (
              <div className="flex h-full items-center justify-center">
                <Package className="h-24 w-24 text-muted-foreground/30" />
              </div>
            )}
          </div>
        </motion.div>

        <motion.div
          initial={{ opacity: 0, x: 20 }}
          animate={{ opacity: 1, x: 0 }}
          transition={{ delay: 0.1 }}
          className="flex flex-col justify-center"
        >
          <Badge variant="secondary" className="w-fit mb-4 backdrop-blur-sm bg-secondary/80">
            {product.categoryName}
          </Badge>

          <h1 className="text-3xl font-bold tracking-tight">
            {product.name}
          </h1>

          <div className="flex items-center gap-4 mt-4">
            <div className="flex items-center gap-1">
              <StarRating
                rating={Math.round(reviews?.averageRating || 0)}
              />
              <span className="text-sm text-muted-foreground ml-1">
                ({reviews?.totalReviews || 0} reviews)
              </span>
            </div>
          </div>

          <div className="mt-6 flex items-baseline gap-2">
            <span className="text-4xl font-bold tracking-tight tabular-nums">
              {formatPrice(product.price)}
            </span>
          </div>

          <Separator className="my-6" />

          <p className="text-muted-foreground leading-relaxed">
            {product.description}
          </p>

          <div className="mt-6 grid grid-cols-2 gap-3">
            <div className="flex items-center gap-2.5 p-3 rounded-lg bg-primary/5 backdrop-blur-sm border border-primary/10">
              <Truck className="h-4 w-4 text-primary" />
              <span className="text-sm text-muted-foreground">Free shipping</span>
            </div>
            <div className="flex items-center gap-2.5 p-3 rounded-lg bg-primary/5 backdrop-blur-sm border border-primary/10">
              <Shield className="h-4 w-4 text-primary" />
              <span className="text-sm text-muted-foreground">Secure checkout</span>
            </div>
          </div>

          <Separator className="my-6" />

          <div className="flex items-center gap-2 mb-6">
            <span
              className={`inline-block w-2.5 h-2.5 rounded-full ${
                product.stockQuantity > 0 ? 'bg-green-500 shadow-sm shadow-green-500/30' : 'bg-red-500'
              }`}
            />
            <span className="text-sm font-medium">
              {product.stockQuantity > 0
                ? `In Stock (${product.stockQuantity} available)`
                : 'Out of Stock'}
            </span>
          </div>

          <div className="flex items-center gap-4 mb-6">
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="icon"
                onClick={() => setQuantity(Math.max(1, quantity - 1))}
                disabled={quantity <= 1}
                className="shadow-sm"
              >
                <Minus className="h-4 w-4" />
              </Button>
              <span className="w-10 text-center font-medium tabular-nums">{quantity}</span>
              <Button
                variant="outline"
                size="icon"
                onClick={() =>
                  setQuantity(Math.min(product.stockQuantity, quantity + 1))
                }
                disabled={quantity >= product.stockQuantity}
                className="shadow-sm"
              >
                <Plus className="h-4 w-4" />
              </Button>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <Button
              size="lg"
              className="flex-1 gap-2 shadow-lg shadow-primary/20 hover:shadow-xl hover:shadow-primary/30"
              disabled={product.stockQuantity <= 0}
              onClick={handleAddToCart}
            >
              <ShoppingCart className="h-5 w-5" />
              Add to Cart
            </Button>
            <Button
              variant="outline"
              size="lg"
              onClick={handleToggleWishlist}
              className="shadow-sm"
            >
              <Heart
                className={`h-5 w-5 ${
                  inWishlist ? 'fill-red-500 text-red-500' : ''
                }`}
              />
            </Button>
          </div>
        </motion.div>
      </div>

      <section className="mt-16 lg:mt-24">
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
        >
          <h2 className="text-2xl font-bold mb-6">Customer Reviews</h2>
          {reviews && reviews.reviews.length > 0 ? (
            <div className="space-y-6">
              <div className="flex items-center gap-6 mb-8 p-6 rounded-xl bg-muted/40 border">
                <div className="text-center">
                  <div className="text-5xl font-bold tabular-nums">
                    {reviews.averageRating.toFixed(1)}
                  </div>
                  <div className="mt-1">
                    <StarRating rating={Math.round(reviews.averageRating)} />
                  </div>
                  <p className="text-sm text-muted-foreground mt-1">
                    {reviews.totalReviews} reviews
                  </p>
                </div>
              </div>
              <Separator />
              {reviews.reviews.map((review) => (
                <div key={review.id} className="space-y-2 p-4 rounded-lg border shadow-sm">
                  <div className="flex items-center gap-2">
                    <span className="font-medium text-sm">
                      {review.userName}
                    </span>
                    <StarRating rating={review.rating} />
                  </div>
                  {review.comment && (
                    <p className="text-sm text-muted-foreground">
                      {review.comment}
                    </p>
                  )}
                  <p className="text-xs text-muted-foreground">
                    {new Date(review.createdAt).toLocaleDateString()}
                  </p>
                </div>
              ))}
            </div>
          ) : (
            <p className="text-muted-foreground">
              No reviews yet. Be the first to review!
            </p>
          )}
        </motion.div>
      </section>

      <RelatedProducts
        categoryName={product.categoryName}
        excludeId={product.id}
      />
    </div>
  );
}
