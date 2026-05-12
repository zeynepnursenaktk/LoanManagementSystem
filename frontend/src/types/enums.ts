// .NET enum sözleşmesi
// Request tarafında numeric int kullanılır; response'larda backend lokalize string döner.

export enum LoanType {
  Personal = 0, // İhtiyaç Kredisi
  Education = 1, // Eğitim Kredisi
  Vehicle = 2, // Araç / Taşıt Kredisi
}

export const LoanTypeLabels: Record<LoanType, string> = {
  [LoanType.Personal]: "İhtiyaç Kredisi",
  [LoanType.Education]: "Eğitim Kredisi",
  [LoanType.Vehicle]: "Araç Kredisi",
};

export type LoanStatus = "Active" | "Closed";

export const LoanStatusLabels: Record<LoanStatus, string> = {
  Active: "Aktif",
  Closed: "Kapatıldı",
};

export type InstallmentStatus = "Unpaid" | "Paid" | "Overdue";

export const InstallmentStatusLabels: Record<InstallmentStatus, string> = {
  Unpaid: "Ödenmedi",
  Paid: "Ödendi",
  Overdue: "Gecikmiş",
};

export type AppRole = "Admin" | "Customer";
