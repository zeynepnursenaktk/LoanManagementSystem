import { apiClient } from "@/api/axiosClient";
import type { CreditScoreResponseDto } from "@/types";

const BASE = "/api/CreditScores";

export const creditScoreService = {
  async getForCustomer(customerId: number): Promise<CreditScoreResponseDto> {
    const { data } = await apiClient.get<CreditScoreResponseDto>(`${BASE}/${customerId}`);
    return data;
  },
};
