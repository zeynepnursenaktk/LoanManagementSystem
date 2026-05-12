import type { LoanSummaryDto, LoanDetailSummaryDto } from "./loan";

export interface CreateCustomerDto {
  firstName: string;
  lastName: string;
  identityNumber: string;
  email: string;
  phoneNumber?: string | null;
}

export interface UpdateCustomerDto {
  email: string;
  phoneNumber?: string | null;
}

export interface CustomerListDto {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string | null;
  totalLoans: number;
  loans: LoanSummaryDto[];
  /**
   * Müşteri soft-delete edilmişse true. Admin tablosunda "Aktif/Pasif" badge'i
   * ve dinamik aksiyon butonu (Sil ↔ Aktife Çevir) için kullanılır.
   * Backend: `LoanManagement.Entities.DTOs.CustomerListDto.IsDeleted` (v1.4+).
   */
  isDeleted: boolean;
  /** Soft-delete zamanı (UTC, ISO-8601). `isDeleted=false` ise `null`. */
  deletedAtUtc: string | null;
}

export interface CustomerResponseDto extends CustomerListDto {
  identityNumber: string;
}

export interface CustomerSummaryDto {
  customerId: number;
  fullName: string;
  totalLoans: number;
  activeLoans: number;
  closedLoans: number;
  totalDebt: number;
  totalPaid: number;
  totalInstallments: number;
  paidInstallments: number;
  unpaidInstallments: number;
  overdueInstallments: number;
  loans: LoanDetailSummaryDto[];
}

export interface CreateCustomerResponse {
  id: number;
  message: string;
}

export interface MessageResponse {
  message: string;
}
