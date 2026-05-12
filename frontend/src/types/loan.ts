import type { InstallmentDto } from "./installment";
import { LoanType, type LoanStatus } from "./enums";

export interface LoanRequestDto {
  customerId: number;
  amount: number;
  tenor: number;
  /** Yıllık kar oranı, yüzde (örn. 24 = %24) */
  profitRate: number;
  /** ISO datetime */
  startDate: string;
  loanType: LoanType;
}

export interface LoanResponseDto {
  id: number;
  customerId: number;
  customerFullName: string;
  loanTypeName: string;
  amount: number;
  tenor: number;
  /** Yüzde olarak (örn 24 = %24) */
  profitRate: number;
  totalPayable: number;
  startDate: string;
  status: LoanStatus;
  installments: InstallmentDto[];
}

export interface LoanSummaryDto {
  loanId: number;
  loanTypeName: string;
  status: LoanStatus;
  amount: number;
  tenor: number;
  startDate: string | null;
  totalInstallments: number;
  paidInstallments: number;
  unpaidInstallments: number;
  overdueInstallments: number;
  remainingDebt: number;
}

export interface LoanDetailSummaryDto {
  loanId: number;
  loanTypeName: string;
  status: LoanStatus;
  amount: number;
  totalPayable: number;
  totalPaid: number;
  remainingDebt: number;
  totalInstallments: number;
  paidInstallments: number;
  unpaidInstallments: number;
  overdueInstallments: number;
}
