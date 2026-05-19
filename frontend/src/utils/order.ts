export function getOrderStatusColor(
  status: number
): 'default' | 'secondary' | 'destructive' | 'outline' {
  switch (status) {
    case 0: // Pending
      return 'secondary';
    case 1: // PaymentProcessing
      return 'secondary';
    case 2: // Paid
      return 'default';
    case 3: // Shipped
      return 'default';
    case 4: // Delivered
      return 'default';
    case 5: // Cancelled
      return 'destructive';
    case 6: // Refunded
      return 'outline';
    default:
      return 'outline';
  }
}
