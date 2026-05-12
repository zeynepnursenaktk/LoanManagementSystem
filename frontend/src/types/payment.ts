export type PaymentStatus = "Succeeded" | "Declined" | "Failed";

/** Backend `PaymentDeclineCodes` ile aynı anahtarlar (v1.1). */
export type PaymentDeclineCode =
  | "insufficient_funds"
  | "card_declined"
  | "expired_card"
  | "incorrect_cvc"
  | "processing_error"
  | "gateway_timeout"
  | "invalid_amount"
  | "fraudulent";

export interface PaymentRequestDto {
  loanId: number;
}

export interface PaymentResponseDto {
  status: PaymentStatus;
  declineCode: PaymentDeclineCode | null;
  providerName: string;
  providerReference: string;
  message: string;
  paymentId: number;
  customerId: number;
  customerName: string;
  loanId: number;
  loanTypeName: string;
  installmentId: number;
  installmentNumber: number;
  paidAmount: number;
  paymentDate: string;
  processedAtUtc: string;
  isLoanClosed: boolean;
}

/** HTTP 402 Payment Required yanıt gövdesi (Stripe-uyumlu). */
export interface PaymentDeclinedErrorBody {
  statusCode?: number;
  message: string;
  details?: {
    status?: PaymentStatus;
    declineCode?: PaymentDeclineCode;
    providerName?: string;
  };
  // GlobalExceptionHandler dışında, controller içi catch'ten gelen düz yapı da olabilir:
  status?: PaymentStatus;
  declineCode?: PaymentDeclineCode;
  providerName?: string;
}
