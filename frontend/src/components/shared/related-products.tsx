import { useQuery } from '@tanstack/react-query';
import { ShoppingBag } from 'lucide-react';
import { Link } from 'react-router-dom';
import { productsApi } from '@/api/products';
import { ProductCard } from './product-card';

interface RelatedProductsProps {
  categoryName: string;
  excludeId: number;
}

export function RelatedProducts({
  categoryName,
  excludeId,
}: RelatedProductsProps) {
  const { data, isLoading } = useQuery({
    queryKey: ['products', 'related', categoryName],
    queryFn: () =>
      productsApi.list({ search: categoryName, pageSize: 4 }),
    enabled: !!categoryName,
  });

  const products =
    data?.data?.items.filter((p) => p.id !== excludeId).slice(0, 4) ?? [];

  if (!isLoading && products.length === 0) return null;

  if (isLoading) {
    return (
      <section className="mt-12">
        <h2 className="text-2xl font-bold mb-6">Related Products</h2>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-6">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="animate-pulse space-y-3">
              <div className="aspect-square rounded-xl bg-muted" />
              <div className="h-4 w-2/3 rounded bg-muted" />
              <div className="h-4 w-1/3 rounded bg-muted" />
            </div>
          ))}
        </div>
      </section>
    );
  }

  return (
    <section className="mt-12">
      <h2 className="text-2xl font-bold mb-6">Related Products</h2>
      <div className="grid grid-cols-2 md:grid-cols-4 gap-6">
        {products.map((product, index) => (
          <ProductCard key={product.id} product={product} index={index} />
        ))}
      </div>
    </section>
  );
}
