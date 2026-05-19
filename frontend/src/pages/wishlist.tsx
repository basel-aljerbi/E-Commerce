import { useWishlist } from '@/hooks/useWishlist';
import { useRemoveFromWishlist } from '@/hooks/useWishlist';
import { ProductGrid } from '@/components/shared/product-grid';
import { Heart } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';

export default function WishlistPage() {
  const { data, isLoading } = useWishlist();
  const removeFromWishlist = useRemoveFromWishlist();
  const items = data?.data ?? [];
  const wishlistIds = new Set(items.map((w) => w.productId));

  const products = items.map((w) => ({
    id: w.productId,
    name: w.productName,
    description: '',
    price: w.productPrice,
    stockQuantity: 0,
    imageUrl: w.imageUrl,
    categoryName: '',
  }));

  const handleToggleWishlist = (productId: number) => {
    removeFromWishlist.mutate(productId);
  };

  if (!isLoading && items.length === 0) {
    return (
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-20 text-center">
        <Heart className="h-16 w-16 mx-auto text-muted-foreground/30 mb-4" />
        <h1 className="text-3xl font-bold mb-2">Your wishlist is empty</h1>
        <p className="text-muted-foreground mb-6">
          Save your favorite items here
        </p>
        <Link to="/products">
          <Button>Browse Products</Button>
        </Link>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-8">
      <h1 className="text-3xl font-bold mb-8">My Wishlist</h1>
      <ProductGrid
        products={products}
        isLoading={isLoading}
        wishlistIds={wishlistIds}
        onToggleWishlist={handleToggleWishlist}
      />
    </div>
  );
}
