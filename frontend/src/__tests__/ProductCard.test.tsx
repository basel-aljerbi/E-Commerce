import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ProductCard } from '@/components/shared/product-card';
import type { ProductDto } from '@/types';

function Wrapper({ children }: { children: React.ReactNode }) {
  const qc = new QueryClient();
  return (
    <QueryClientProvider client={qc}>
      <BrowserRouter>{children}</BrowserRouter>
    </QueryClientProvider>
  );
}

const mockProduct: ProductDto = {
  id: 1,
  name: 'Test Product',
  description: 'A test product',
  price: 29.99,
  stockQuantity: 10,
  imageUrl: null,
  categoryName: 'Electronics',
};

describe('ProductCard', () => {
  it('renders product name and price', () => {
    render(
      <Wrapper>
        <ProductCard product={mockProduct} />
      </Wrapper>
    );
    expect(screen.getByText('Test Product')).toBeInTheDocument();
    expect(screen.getByText('$29.99')).toBeInTheDocument();
  });

  it('renders category name', () => {
    render(
      <Wrapper>
        <ProductCard product={mockProduct} />
      </Wrapper>
    );
    expect(screen.getByText('Electronics')).toBeInTheDocument();
  });

  it('shows Add to Cart button', () => {
    render(
      <Wrapper>
        <ProductCard product={mockProduct} />
      </Wrapper>
    );
    expect(screen.getByText('Add to Cart')).toBeInTheDocument();
  });
});
