import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { ArrowRight, ShoppingBag, Truck, Shield, RefreshCw, ImageIcon } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { ProductCard } from '@/components/shared/product-card';
import { useProducts } from '@/hooks/useProducts';
import { getImageUrl } from '@/lib/utils';
import { useAdminCategories } from '@/hooks/useAdmin';

const features = [
  { icon: Truck, title: 'Free Shipping', desc: 'On orders over $50' },
  { icon: Shield, title: 'Secure Payment', desc: '100% secure checkout' },
  { icon: RefreshCw, title: 'Easy Returns', desc: '30-day return policy' },
];

export default function HomePage() {
  const { data: featuredData, isLoading: productsLoading } = useProducts({
    pageSize: 8,
    sortBy: 'date',
    sortOrder: 'desc',
  });

  const { data: categoriesData, isLoading: catsLoading } = useAdminCategories();
  const products = featuredData?.data?.items ?? [];
  const categories = categoriesData?.data ?? [];

  return (
    <div>
      {/* Hero */}
      <section className="relative overflow-hidden bg-gradient-to-br from-primary/[0.04] via-background to-background">
        <div className="absolute inset-0 bg-grid-small-white/5 [mask-image:radial-gradient(ellipse_at_center,white,transparent_70%)]" />
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-20 md:py-32 relative">
          <div className="grid md:grid-cols-2 gap-12 items-center">
            <motion.div
              initial={{ opacity: 0, x: -20 }}
              animate={{ opacity: 1, x: 0 }}
              transition={{ duration: 0.6 }}
            >
              <span className="inline-flex items-center gap-1.5 px-4 py-1.5 rounded-full bg-primary/10 text-primary text-sm font-medium mb-6 border border-primary/20 shadow-sm">
                <span className="w-2 h-2 rounded-full bg-primary animate-pulse" />
                New Collection 2026
              </span>
              <h1 className="text-4xl md:text-6xl font-bold tracking-tight mb-6 leading-tight">
                Discover Premium
                <span className="text-gradient block mt-1"> Products</span>
              </h1>
              <p className="text-lg text-muted-foreground mb-8 max-w-lg leading-relaxed">
                Shop the latest trends with confidence. Premium quality products
                at unbeatable prices, delivered to your doorstep.
              </p>
              <div className="flex items-center gap-4">
                <Link to="/products">
                  <Button size="lg" className="gap-2 shadow-xl shadow-primary/20 hover:shadow-2xl hover:shadow-primary/30">
                    Shop Now
                    <ArrowRight className="h-4 w-4" />
                  </Button>
                </Link>
                <Link to="/products?sortBy=date&sortOrder=desc">
                  <Button variant="outline" size="lg" className="shadow-sm">
                    New Arrivals
                  </Button>
                </Link>
              </div>
            </motion.div>
            <motion.div
              initial={{ opacity: 0, x: 20 }}
              animate={{ opacity: 1, x: 0 }}
              transition={{ duration: 0.6, delay: 0.2 }}
              className="hidden md:block"
            >
              <div className="relative">
                <div className="absolute -inset-4 bg-gradient-to-r from-primary/20 via-primary/5 to-transparent rounded-[2.5rem] blur-xl" />
                <div className="relative aspect-square rounded-[2rem] bg-gradient-to-br from-primary/10 via-secondary/30 to-background border shadow-2xl flex items-center justify-center overflow-hidden">
                  <div className="absolute inset-0 bg-grid-small-white/10" />
                  <motion.div
                    animate={{ y: [0, -10, 0] }}
                    transition={{ duration: 6, repeat: Infinity, ease: 'easeInOut' }}
                  >
                    <ShoppingBag className="h-48 w-48 text-primary/20" />
                  </motion.div>
                </div>
              </div>
            </motion.div>
          </div>
        </div>
      </section>

      {/* Features */}
      <section className="border-y bg-muted/40">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-12">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            {features.map((feature, i) => {
              const Icon = feature.icon;
              return (
                <motion.div
                  key={feature.title}
                  initial={{ opacity: 0, y: 20 }}
                  whileInView={{ opacity: 1, y: 0 }}
                  viewport={{ once: true }}
                  transition={{ delay: i * 0.1 }}
                  className="group flex items-center gap-4 p-4 rounded-xl hover:bg-muted/60 transition-colors"
                >
                  <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-primary/10 to-primary/5 ring-1 ring-primary/10 group-hover:shadow-md transition-shadow">
                    <Icon className="h-6 w-6 text-primary" />
                  </div>
                  <div>
                    <h3 className="font-semibold">{feature.title}</h3>
                    <p className="text-sm text-muted-foreground">{feature.desc}</p>
                  </div>
                </motion.div>
              );
            })}
          </div>
        </div>
      </section>

      {/* Featured Products */}
      <section className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-16 lg:py-24">
        <div className="flex items-end justify-between mb-10">
          <div>
            <h2 className="text-3xl lg:text-4xl font-bold tracking-tight">Featured Products</h2>
            <p className="text-muted-foreground mt-2">Our latest and greatest products</p>
          </div>
          <Link to="/products" className="hidden sm:block">
            <Button variant="outline" className="gap-2 group">
              View All
              <ArrowRight className="h-4 w-4 transition-transform group-hover:translate-x-0.5" />
            </Button>
          </Link>
        </div>
        {productsLoading ? (
          <div className="grid grid-cols-2 md:grid-cols-4 gap-6">
            {Array.from({ length: 4 }).map((_, i) => (
              <div key={i} className="space-y-3">
                <div className="aspect-square rounded-xl skeleton-pulse" />
                <div className="h-4 w-2/3 rounded skeleton-pulse" />
                <div className="h-4 w-1/3 rounded skeleton-pulse" />
              </div>
            ))}
          </div>
        ) : products.length > 0 ? (
          <div className="grid grid-cols-2 md:grid-cols-4 gap-6">
            {products.slice(0, 4).map((product, index) => (
              <ProductCard key={product.id} product={product} index={index} />
            ))}
          </div>
        ) : (
          <p className="text-muted-foreground text-center py-12">No products available yet.</p>
        )}
        <div className="mt-8 text-center sm:hidden">
          <Link to="/products">
            <Button variant="outline" className="gap-2">
              View All Products
              <ArrowRight className="h-4 w-4" />
            </Button>
          </Link>
        </div>
      </section>

      {/* Shop by Category */}
      <section className="bg-muted/40 py-16 lg:py-24">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-10">
            <h2 className="text-3xl lg:text-4xl font-bold tracking-tight">Shop by Category</h2>
            <p className="text-muted-foreground mt-2">Find exactly what you are looking for</p>
          </div>
          {catsLoading ? (
            <div className="grid grid-cols-2 md:grid-cols-4 gap-6">
              {Array.from({ length: 4 }).map((_, i) => (
                <div key={i} className="aspect-square rounded-xl skeleton-pulse" />
              ))}
            </div>
          ) : categories.length > 0 ? (
            <div className="grid grid-cols-2 md:grid-cols-4 gap-6">
              {categories.map((cat: any, i: number) => (
                <motion.div
                  key={cat.id}
                  initial={{ opacity: 0, y: 20 }}
                  whileInView={{ opacity: 1, y: 0 }}
                  viewport={{ once: true }}
                  transition={{ delay: i * 0.08 }}
                >
                  <Link
                    to={`/products?categoryId=${cat.id}`}
                    className="group relative block aspect-square overflow-hidden rounded-2xl bg-muted shadow-lg hover:shadow-2xl transition-shadow duration-300"
                  >
                    {cat.imageUrl ? (
                      <img
                        src={getImageUrl(cat.imageUrl)}
                        alt={cat.name}
                        className="absolute inset-0 h-full w-full object-cover transition-transform duration-700 group-hover:scale-110"
                      />
                    ) : (
                      <div className="absolute inset-0 flex items-center justify-center bg-gradient-to-br from-primary/10 to-secondary/30">
                        <ImageIcon className="h-16 w-16 text-muted-foreground/40" />
                      </div>
                    )}
                    <div className="absolute inset-0 bg-gradient-to-t from-black/70 via-black/20 to-transparent" />
                    <div className="absolute bottom-4 left-4 right-4">
                      <h3 className="text-white font-bold text-lg drop-shadow-sm">{cat.name}</h3>
                      <span className="text-white/80 text-sm inline-flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-all duration-300 translate-y-1 group-hover:translate-y-0">
                        Shop Now <ArrowRight className="h-3 w-3" />
                      </span>
                    </div>
                  </Link>
                </motion.div>
              ))}
            </div>
          ) : (
            <p className="text-muted-foreground text-center">No categories yet.</p>
          )}
        </div>
      </section>

      {/* Newsletter */}
      <section className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-16 lg:py-24">
        <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-primary via-primary/90 to-primary/80 p-8 md:p-12 lg:p-16 shadow-2xl">
          <div className="absolute inset-0 bg-grid-small-white/10" />
          <div className="absolute top-0 right-0 w-64 h-64 bg-white/5 rounded-full blur-3xl -translate-y-1/2 translate-x-1/2" />
          <div className="relative z-10">
            <h2 className="text-3xl md:text-4xl font-bold text-primary-foreground mb-4">
              Join Our Newsletter
            </h2>
            <p className="text-primary-foreground/80 mb-6 max-w-md text-lg">
              Get exclusive access to new arrivals, sales, and member-only pricing.
            </p>
            <div className="flex max-w-md gap-2">
              <input
                type="email"
                placeholder="Enter your email"
                className="flex-1 rounded-lg bg-white/15 border border-white/20 px-4 py-2.5 text-white placeholder:text-white/60 text-sm focus:outline-none focus:ring-2 focus:ring-white/40 backdrop-blur-sm transition-shadow"
              />
              <Button variant="secondary" size="lg" className="shrink-0 shadow-lg">
                Subscribe
              </Button>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
