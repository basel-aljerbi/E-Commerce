export interface PaymentRequestDto {
  orderId: number;
}

export interface PaymentResponseDto {
  clientSecret: string;
  status: string;
  message: string;
}
