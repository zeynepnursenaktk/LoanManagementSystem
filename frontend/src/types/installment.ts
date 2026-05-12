import type { InstallmentStatus } from "./enums";

export interface InstallmentDto {
  id: number;
  loanId: number;
  installmentNumber: number;
  amount: number;
  dueDate: string;
  status: InstallmentStatus;
  isPaid: boolean;
  paidAmount: number | null;
  paymentDate: string | null;
}

export interface UnpaidInstallmentDto {
  id: number;
  loanId: number;
  installmentNumber: number;
  amount: number;
  dueDate: string;
}

export interface UpdateOverdueResponse {
  message: string;
  updatedCount: number;
}
