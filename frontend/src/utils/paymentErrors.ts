import { AxiosError } from "axios";
import type { PaymentDeclineCode, PaymentDeclinedErrorBody, PaymentStatus } from "@/types";

export interface ParsedPaymentError {
  status: PaymentStatus | "Unknown";
  declineCode: PaymentDeclineCode | null;
  providerName: string;
  /** Son kullanıcıya gösterilecek, dile lokalize edilmiş tek satır. */
  userMessage: string;
  /** Backend'in döndürdüğü teknik açıklama (loglama / detay görünümü). */
  rawMessage: string;
  /** UI severity. */
  severity: "error" | "warning" | "info";
  /** Tekrar denenebilir mi? (network/timeout için true). */
  retryable: boolean;
  httpStatus?: number;
}

const FALLBACK: ParsedPaymentError = {
  status: "Unknown",
  declineCode: null,
  providerName: "",
  userMessage: "Ödeme sırasında bilinmeyen bir hata oluştu. Lütfen tekrar deneyin.",
  rawMessage: "",
  severity: "error",
  retryable: true,
};

const DECLINE_MESSAGES: Record<PaymentDeclineCode, { msg: string; severity: ParsedPaymentError["severity"]; retryable: boolean }> = {
  insufficient_funds: {
    msg: "Kart limiti yetersiz. Lütfen farklı bir ödeme yöntemi deneyin.",
    severity: "error",
    retryable: false,
  },
  card_declined: {
    msg: "Ödemeniz bankanız tarafından reddedildi. Lütfen bankanızla iletişime geçin.",
    severity: "error",
    retryable: false,
  },
  expired_card: {
    msg: "Kartın son kullanma tarihi geçmiş. Lütfen geçerli bir kart kullanın.",
    severity: "error",
    retryable: false,
  },
  incorrect_cvc: {
    msg: "Kart güvenlik kodu (CVC) hatalı.",
    severity: "warning",
    retryable: true,
  },
  fraudulent: {
    msg: "İşlem güvenlik nedeniyle reddedildi. Detaylı bilgi için bankanızla iletişime geçin.",
    severity: "error",
    retryable: false,
  },
  invalid_amount: {
    msg: "Ödeme tutarı geçersiz. Lütfen destek ekibimizle iletişime geçin.",
    severity: "error",
    retryable: false,
  },
  processing_error: {
    msg: "Ödeme sağlayıcısı tarafında beklenmeyen bir hata oluştu. Lütfen kısa süre sonra tekrar deneyin.",
    severity: "warning",
    retryable: true,
  },
  gateway_timeout: {
    msg: "Ödeme sağlayıcısına şu an ulaşılamıyor. Lütfen biraz sonra tekrar deneyin.",
    severity: "warning",
    retryable: true,
  },
};

export function parsePaymentError(error: unknown): ParsedPaymentError {
  const ax = error as AxiosError<PaymentDeclinedErrorBody | undefined>;
  const status = ax?.response?.status;
  const body = ax?.response?.data;

  if (!body) {
    return {
      ...FALLBACK,
      userMessage:
        status === 0 || ax?.code === "ERR_NETWORK"
          ? "Sunucuya ulaşılamadı. Lütfen internet bağlantınızı kontrol edin."
          : FALLBACK.userMessage,
      retryable: true,
      httpStatus: status,
    };
  }

  // Body iki olası şekilde gelebilir:
  // 1) GlobalExceptionHandler: { message, details: { status, declineCode, providerName } }
  // 2) Controller-içi catch: { status, declineCode, providerName, message }
  const declineCode = (body.details?.declineCode ?? body.declineCode ?? null) as PaymentDeclineCode | null;
  const respStatus = (body.details?.status ?? body.status ?? "Failed") as PaymentStatus;
  const providerName = body.details?.providerName ?? body.providerName ?? "";
  const rawMessage = body.message ?? "";

  if (declineCode && declineCode in DECLINE_MESSAGES) {
    const entry = DECLINE_MESSAGES[declineCode];
    return {
      status: respStatus,
      declineCode,
      providerName,
      userMessage: entry.msg,
      rawMessage,
      severity: entry.severity,
      retryable: entry.retryable,
      httpStatus: status,
    };
  }

  // 503 - sağlayıcı down (ödeme servisi için olağandışı ama yine de ele al)
  if (status === 503 || status === 504) {
    return {
      status: "Failed",
      declineCode: "gateway_timeout",
      providerName,
      userMessage: "Ödeme sağlayıcısına ulaşılamadı. Lütfen biraz sonra tekrar deneyin.",
      rawMessage,
      severity: "warning",
      retryable: true,
      httpStatus: status,
    };
  }

  // 400 / 403 / 404 — backend's `message` alanını doğrudan göster.
  return {
    status: respStatus,
    declineCode,
    providerName,
    userMessage: rawMessage || FALLBACK.userMessage,
    rawMessage,
    severity: "error",
    retryable: false,
    httpStatus: status,
  };
}
