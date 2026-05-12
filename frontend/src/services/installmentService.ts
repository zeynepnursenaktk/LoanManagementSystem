import { apiClient } from "@/api/axiosClient";
import type {
  InstallmentDto,
  UnpaidInstallmentDto,
  UpdateOverdueResponse,
} from "@/types";

const BASE = "/api/Installments";

export const installmentService = {
  async listByLoan(loanId: number): Promise<InstallmentDto[]> {
    const { data } = await apiClient.get<InstallmentDto[]>(`${BASE}/by-loan/${loanId}`);
    return data;
  },

  async getById(id: number): Promise<InstallmentDto> {
    const { data } = await apiClient.get<InstallmentDto>(`${BASE}/${id}`);
    return data;
  },

  async unpaidByCustomer(customerId: number): Promise<UnpaidInstallmentDto[]> {
    const { data } = await apiClient.get<UnpaidInstallmentDto[]>(
      `${BASE}/unpaid/by-customer/${customerId}`,
    );
    return data;
  },

  async overdueByCustomer(customerId: number): Promise<UnpaidInstallmentDto[]> {
    const { data } = await apiClient.get<UnpaidInstallmentDto[]>(
      `${BASE}/overdue/by-customer/${customerId}`,
    );
    return data;
  },

  async refreshOverdue(): Promise<UpdateOverdueResponse> {
    const { data } = await apiClient.post<UpdateOverdueResponse>(`${BASE}/update-overdue`);
    return data;
  },
};
