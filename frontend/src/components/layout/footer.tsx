import { Link } from 'react-router-dom';
import { Store, Heart, Mail } from 'lucide-react';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';

export function Footer() {
  return (
    <footer className="border-t bg-muted/30">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="relative">
          <div className="absolute top-0 left-1/4 right-1/4 h-px bg-gradient-to-r from-transparent via-primary/20 to-transparent" />
          <div className="py-12 lg:py-16 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-8 lg:gap-12">
            <div className="space-y-4">
              <Link to="/" className="flex items-center gap-2">
                <div className="flex items-center justify-center w-9 h-9 rounded-xl bg-gradient-to-br from-primary to-primary/70 text-primary-foreground shadow-sm">
                  <Store className="h-5 w-5" />
                </div>
                <span className="font-bold text-lg">ShopHub</span>
              </Link>
              <p className="text-sm text-muted-foreground leading-relaxed max-w-xs">
                Your premium destination for quality products. We curate the best items to make your shopping experience seamless and enjoyable.
              </p>
            </div>

            <div>
              <h3 className="font-semibold text-sm mb-4 flex items-center gap-2">
                <span className="h-0.5 w-5 bg-primary rounded-full" />
                Shop
              </h3>
              <ul className="space-y-2.5">
                <li>
                  <Link to="/products" className="text-sm text-muted-foreground hover:text-foreground transition-colors">
                    All Products
                  </Link>
                </li>
                <li>
                  <Link to="/products?categoryId=1" className="text-sm text-muted-foreground hover:text-foreground transition-colors">
                    Electronics
                  </Link>
                </li>
                <li>
                  <Link to="/products?categoryId=2" className="text-sm text-muted-foreground hover:text-foreground transition-colors">
                    Clothing
                  </Link>
                </li>
                <li>
                  <Link to="/products?categoryId=3" className="text-sm text-muted-foreground hover:text-foreground transition-colors">
                    Books
                  </Link>
                </li>
              </ul>
            </div>

            <div>
              <h3 className="font-semibold text-sm mb-4 flex items-center gap-2">
                <span className="h-0.5 w-5 bg-primary rounded-full" />
                Support
              </h3>
              <ul className="space-y-2.5">
                <li>
                  <span className="text-sm text-muted-foreground cursor-default">Help Center</span>
                </li>
                <li>
                  <span className="text-sm text-muted-foreground cursor-default">Shipping Info</span>
                </li>
                <li>
                  <span className="text-sm text-muted-foreground cursor-default">Returns</span>
                </li>
                <li>
                  <span className="text-sm text-muted-foreground cursor-default">Contact</span>
                </li>
              </ul>
            </div>

            <div>
              <h3 className="font-semibold text-sm mb-4 flex items-center gap-2">
                <span className="h-0.5 w-5 bg-primary rounded-full" />
                Stay Updated
              </h3>
              <p className="text-sm text-muted-foreground mb-3">
                Get the latest drops and exclusive offers.
              </p>
              <div className="flex gap-2">
                <Input
                  type="email"
                  placeholder="your@email.com"
                  className="h-9 text-sm"
                  aria-label="Email for newsletter"
                />
                <Button size="sm" className="shrink-0" aria-label="Subscribe">
                  <Mail className="h-4 w-4" />
                </Button>
              </div>
            </div>
          </div>
        </div>

        <div className="border-t py-6 flex flex-col sm:flex-row items-center justify-between gap-4">
          <p className="text-xs text-muted-foreground">
            &copy; {new Date().getFullYear()} ShopHub. All rights reserved.
          </p>
          <div className="flex items-center gap-1 text-xs text-muted-foreground">
            Made with <Heart className="h-3 w-3 text-red-500 fill-red-500" /> by ShopHub
          </div>
        </div>
      </div>
    </footer>
  );
}
