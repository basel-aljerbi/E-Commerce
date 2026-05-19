import { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { motion } from 'framer-motion';
import {
  SlidersHorizontal,
  Grid3X3,
  List,
  X,
  Search,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { ProductGrid } from '@/components/shared/product-grid';
import { Pagination } from '@/components/shared/pagination';
import { Skeleton } from '@/components/ui/skeleton';
import { useProducts } from '@/hooks/useProducts';
import type { ProductQueryParams } from '@/types';

const sortOptions = [
  { label: 'Newest', value: 'date_desc' },
  { label: 'Price: Low to High', value: 'price_asc' },
  { label: 'Price: High to Low', value: 'price_desc' },
  { label: 'Name: A-Z', value: 'name_asc' },
  { label: 'Name: Z-A', value: 'name_desc' },
];

const containerVariants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: { staggerChildren: 0.05 },
  },
};

const itemVariants = {
  hidden: { opacity: 0, y: 20 },
  visible: { opacity: 1, y: 0 },
};

export default function ProductsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid');
  const [showFilters, setShowFilters] = useState(false);

  const page = Number(searchParams.get('pageNumber')) || 1;
  const search = searchParams.get('search') || '';
  const sortBy = searchParams.get('sortBy') || 'date';
  const sortOrder = (searchParams.get('sortOrder') || 'desc') as 'asc' | 'desc';
  const categoryId = searchParams.get('categoryId')
    ? Number(searchParams.get('categoryId'))
    : undefined;
  const minPrice = searchParams.get('minPrice')
    ? Number(searchParams.get('minPrice'))
    : undefined;
  const maxPrice = searchParams.get('maxPrice')
    ? Number(searchParams.get('maxPrice'))
    : undefined;

  const params: ProductQueryParams = {
    pageNumber: page,
    pageSize: 12,
    search: search || undefined,
    sortBy,
    sortOrder,
    categoryId,
    minPrice,
    maxPrice,
  };

  const { data, isLoading } = useProducts(params);

  const [localSearch, setLocalSearch] = useState(search);
  const [localMinPrice, setLocalMinPrice] = useState(
    minPrice?.toString() || ''
  );
  const [localMaxPrice, setLocalMaxPrice] = useState(
    maxPrice?.toString() || ''
  );

  useEffect(() => {
    setLocalSearch(search);
  }, [search]);

  const updateParams = (updates: Record<string, string | undefined>) => {
    const newParams = new URLSearchParams(searchParams);
    Object.entries(updates).forEach(([key, value]) => {
      if (value) newParams.set(key, value);
      else newParams.delete(key);
    });
    if (updates.pageNumber === undefined && !updates.pageNumber) {
      newParams.set('pageNumber', '1');
    }
    setSearchParams(newParams);
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    updateParams({ search: localSearch || undefined, pageNumber: '1' });
  };

  const handleSort = (value: string) => {
    const [sb, so] = value.split('_');
    updateParams({ sortBy: sb, sortOrder: so });
  };

  const handlePageChange = (p: number) => {
    updateParams({ pageNumber: String(p) });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const clearFilters = () => {
    setLocalMinPrice('');
    setLocalMaxPrice('');
    setSearchParams({});
  };

  const hasFilters = !!(
    search ||
    categoryId ||
    minPrice ||
    maxPrice
  );

  const result = data?.data;

  return (
    <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-8">
      <motion.div
        initial={{ opacity: 0, y: 10 }}
        animate={{ opacity: 1, y: 0 }}
        className="mb-8"
      >
        <h1 className="text-3xl font-bold tracking-tight">Products</h1>
        <p className="text-muted-foreground mt-1">
          {result
            ? `Showing ${result.items.length} of ${result.totalCount} products`
            : 'Browse our collection'}
        </p>
      </motion.div>

      <div className="flex flex-col lg:flex-row gap-8">
        <motion.aside
          initial={{ opacity: 0, x: -10 }}
          animate={{ opacity: 1, x: 0 }}
          className="lg:w-64 shrink-0"
        >
          <div className="flex items-center justify-between lg:hidden mb-4">
            <Button
              variant="outline"
              onClick={() => setShowFilters(!showFilters)}
              className="gap-2"
            >
              <SlidersHorizontal className="h-4 w-4" />
              Filters
            </Button>
            {hasFilters && (
              <Button variant="ghost" size="sm" onClick={clearFilters}>
                <X className="h-4 w-4 mr-1" />
                Clear
              </Button>
            )}
          </div>

          <motion.div
            variants={containerVariants}
            initial="hidden"
            animate={showFilters ? 'visible' : undefined}
            className={`space-y-6 ${showFilters ? 'block' : 'hidden lg:block'}`}
          >
            <motion.div variants={itemVariants}>
              <form onSubmit={handleSearch} className="relative">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input
                  value={localSearch}
                  onChange={(e) => setLocalSearch(e.target.value)}
                  placeholder="Search products..."
                  className="pl-9 rounded-full"
                />
              </form>
            </motion.div>

            <motion.div variants={itemVariants}>
              <h3 className="font-semibold mb-3 text-sm">Price Range</h3>
              <div className="flex items-center gap-2">
                <Input
                  type="number"
                  placeholder="Min"
                  value={localMinPrice}
                  onChange={(e) => setLocalMinPrice(e.target.value)}
                  className="h-9"
                />
                <span className="text-muted-foreground">-</span>
                <Input
                  type="number"
                  placeholder="Max"
                  value={localMaxPrice}
                  onChange={(e) => setLocalMaxPrice(e.target.value)}
                  className="h-9"
                />
              </div>
              <Button
                size="sm"
                variant="outline"
                className="mt-2 w-full"
                onClick={() => {
                  updateParams({
                    minPrice: localMinPrice || undefined,
                    maxPrice: localMaxPrice || undefined,
                    pageNumber: '1',
                  });
                }}
              >
                Apply Price
              </Button>
            </motion.div>

            <motion.div variants={itemVariants}>
              <h3 className="font-semibold mb-3 text-sm">Categories</h3>
              <div className="flex flex-wrap gap-2">
                {['Electronics', 'Clothing', 'Books', 'Sports'].map(
                  (cat) => {
                    const catIdNum =
                      ['Electronics', 'Clothing', 'Books', 'Sports'].indexOf(cat) + 1;
                    const isActive = categoryId === catIdNum;
                    return (
                      <button
                        key={cat}
                        onClick={() => {
                          updateParams({
                            categoryId: isActive ? undefined : String(catIdNum),
                            pageNumber: '1',
                          });
                        }}
                        className={`text-xs font-medium px-3 py-1.5 rounded-full border transition-all duration-200 ${
                          isActive
                            ? 'bg-primary text-primary-foreground border-primary shadow-sm'
                            : 'bg-background text-muted-foreground border-border/60 hover:border-primary/40 hover:text-foreground'
                        }`}
                      >
                        {cat}
                      </button>
                    );
                  }
                )}
              </div>
            </motion.div>

            {hasFilters && (
              <motion.div variants={itemVariants}>
                <Button
                  variant="outline"
                  size="sm"
                  className="w-full"
                  onClick={clearFilters}
                >
                  <X className="h-4 w-4 mr-1" />
                  Clear All Filters
                </Button>
              </motion.div>
            )}
          </motion.div>
        </motion.aside>

        <div className="flex-1 min-w-0">
          <div className="flex items-center justify-between mb-6">
            <div className="flex items-center gap-2">
              <select
                value={`${sortBy}_${sortOrder}`}
                onChange={(e) => handleSort(e.target.value)}
                className="h-9 rounded-lg border border-input bg-background px-3 text-sm shadow-sm focus:ring-2 focus:ring-ring"
              >
                {sortOptions.map((opt) => (
                  <option key={opt.value} value={opt.value}>
                    {opt.label}
                  </option>
                ))}
              </select>
            </div>
            <div className="flex items-center gap-1">
              <Button
                variant={viewMode === 'grid' ? 'secondary' : 'ghost'}
                size="icon"
                onClick={() => setViewMode('grid')}
                aria-label="Grid view"
              >
                <Grid3X3 className="h-4 w-4" />
              </Button>
              <Button
                variant={viewMode === 'list' ? 'secondary' : 'ghost'}
                size="icon"
                onClick={() => setViewMode('list')}
                aria-label="List view"
              >
                <List className="h-4 w-4" />
              </Button>
            </div>
          </div>

          <motion.div
            variants={containerVariants}
            initial="hidden"
            animate="visible"
            key={`${page}-${viewMode}`}
          >
            <ProductGrid
              products={result?.items ?? []}
              isLoading={isLoading}
            />
          </motion.div>

          {result && (
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              className="mt-8"
            >
              <Pagination
                page={result.pageNumber}
                totalPages={result.totalPages}
                onPageChange={handlePageChange}
              />
            </motion.div>
          )}
        </div>
      </div>
    </div>
  );
}
