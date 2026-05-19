export enum OrderStatus {
  Pending = 0,
  PaymentProcessing = 1,
  Paid = 2,
  Shipped = 3,
  Delivered = 4,
  Cancelled = 5,
  Refunded = 6,
}

export interface OrderItemDto {
  id: number;
  productId: number;
  productName: string;
  unitPrice: number;
  quantity: number;
  totalPrice: number;
}

export interface OrderStatusHistoryDto {
  id: number;
  fromStatus: OrderStatus;
  toStatus: OrderStatus;
  reason: string;
  changedAt: string;
}

export interface OrderDto {
  id: number;
  orderDate: string;
  totalAmount: number;
  status: OrderStatus;
  items: OrderItemDto[];
  statusHistory?: OrderStatusHistoryDto[];
}

export const OrderStatusLabel: Record<OrderStatus, string> = {
  [OrderStatus.Pending]: 'Pending',
  [OrderStatus.PaymentProcessing]: 'Payment Processing',
  [OrderStatus.Paid]: 'Paid',
  [OrderStatus.Shipped]: 'Shipped',
  [OrderStatus.Delivered]: 'Delivered',
  [OrderStatus.Cancelled]: 'Cancelled',
  [OrderStatus.Refunded]: 'Refunded',
};
