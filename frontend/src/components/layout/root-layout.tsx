import { Outlet } from 'react-router-dom';
import { motion } from 'framer-motion';
import { Navbar } from './navbar';
import { Footer } from './footer';
import { CartDrawer } from '@/components/shared/cart-drawer';

export function RootLayout() {
  return (
    <div className="flex min-h-screen flex-col bg-mesh">
      <Navbar />
      <motion.main
        initial={{ opacity: 0, y: 8 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.3, ease: 'easeOut' }}
        className="flex-1 pt-16"
      >
        <Outlet />
      </motion.main>
      <Footer />
      <CartDrawer />
    </div>
  );
}
