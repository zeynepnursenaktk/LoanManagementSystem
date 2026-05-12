export type CreditRiskLevel = "VeryLow" | "Low" | "Medium" | "High" | "VeryHigh";

export const CreditRiskLabels: Record<CreditRiskLevel, string> = {
  VeryLow: "Çok Düşük Risk",
  Low: "Düşük Risk",
  Medium: "Orta Risk",
  High: "Yüksek Risk",
  VeryHigh: "Çok Yüksek Risk",
};

/**
 * Skoru oluşturan tek bir bileşen.
 * - code: sabit anahtar (örn. "closed_loan_bonus")
 * - label: kullanıcıya gösterilecek etiket
 * - delta: skoru etkileyen +/- değer (1900 üstü / 300 altı kırpılırsa "clamp" faktörü gelir)
 */
export interface CreditScoreFactor {
  code: string;
  label: string;
  delta: number;
}

export interface CreditScoreResponseDto {
  customerId: number;
  creditScore: number;
  riskLevel: CreditRiskLevel;
  providerName: string;
  providerReference: string;
  queriedAtUtc: string;
  isEligible: boolean;
  /** Production sağlayıcısı sağlamayabilir; sandbox'ta her zaman dolu gelir. */
  factors?: CreditScoreFactor[];
}

export interface CreditScoreProviderError {
  providerError: true;
  message: string;
}
