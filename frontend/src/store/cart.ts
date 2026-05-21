import { create } from 'zustand';
import type { CartItemDto } from '@/types/cart';

interface CartItem {
  productId: number;
  productName: string;
  unitPrice: number;
  quantity: number;
  imageUrl: string | null;
}

interface CartState {
  items: CartItem[];
  isOpen: boolean;
  addItem: (item: CartItem) => void;
  removeItem: (productId: number) => void;
  updateQuantity: (productId: number, quantity: number) => void;
  clearCart: () => void;
  toggleCart: () => void;
  openCart: () => void;
  closeCart: () => void;
  setItems: (items: CartItem[]) => void;
  syncFromServer: (items: CartItemDto[]) => void;
  itemCount: () => number;
  totalAmount: () => number;
}

export const useCartStore = create<CartState>()(
  (set, get) => ({
    items: [],
    isOpen: false,

    addItem: (item: CartItem) => {
      const existing = get().items.find(
        (i) => i.productId === item.productId
      );
      if (existing) {
        set({
          items: get().items.map((i) =>
            i.productId === item.productId
              ? { ...i, quantity: i.quantity + item.quantity }
              : i
          ),
        });
      } else {
        set({ items: [...get().items, item] });
      }
    },

    removeItem: (productId: number) => {
      set({ items: get().items.filter((i) => i.productId !== productId) });
    },

    updateQuantity: (productId: number, quantity: number) => {
      if (quantity <= 0) {
        get().removeItem(productId);
        return;
      }
      set({
        items: get().items.map((i) =>
          i.productId === productId ? { ...i, quantity } : i
        ),
      });
    },

    clearCart: () => set({ items: [] }),
    toggleCart: () => set({ isOpen: !get().isOpen }),
    openCart: () => set({ isOpen: true }),
    closeCart: () => set({ isOpen: false }),

    setItems: (items: CartItem[]) => set({ items }),

    syncFromServer: (items: CartItemDto[]) => {
      const currentItems = get().items;
      set({
        items: items.map((item) => {
          const existing = currentItems.find(
            (ci) => ci.productId === item.productId
          );
          return {
            productId: item.productId,
            productName: item.productName,
            unitPrice: item.unitPrice,
            quantity: item.quantity,
            imageUrl: existing?.imageUrl ?? null,
          };
        }),
      });
    },

    itemCount: () => get().items.reduce((sum, i) => sum + i.quantity, 0),

    totalAmount: () =>
      get().items.reduce((sum, i) => sum + i.unitPrice * i.quantity, 0),
  })
);
