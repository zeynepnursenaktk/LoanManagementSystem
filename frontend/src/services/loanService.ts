import { apiClient } from "@/api/axiosClient";
import type { LoanRequestDto, LoanResponseDto } from "@/types";

const BASE = "/api/Loans";

export const loanService = {
  async list(): Promise<LoanResponseDto[]> {
    const { data } = await apiClient.get<LoanResponseDto[]>(BASE);
    return data;
  },

  async getById(id: number): Promise<LoanResponseDto> {
    const { data } = await apiClient.get<LoanResponseDto>(`${BASE}/${id}`);
    return data;
  },

  async listByCustomer(customerId: number): Promise<LoanResponseDto[]> {
    const { data } = await apiClient.get<LoanResponseDto[]>(`${BASE}/by-customer/${customerId}`);
    return data;
  },

  async create(payload: LoanRequestDto): Promise<LoanResponseDto> {
    const { data } = await apiClient.post<LoanResponseDto>(BASE, payload);
    return data;
  },
};
