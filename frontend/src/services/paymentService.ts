import { apiClient } from "@/api/axiosClient";
import type { PaymentRequestDto, PaymentResponseDto } from "@/types";

const BASE = "/api/Payments";

export const paymentService = {
  async pay(payload: PaymentRequestDto): Promise<PaymentResponseDto> {
    const { data } = await apiClient.post<PaymentResponseDto>(BASE, payload);
    return data;
  },

  async listAll(): Promise<PaymentResponseDto[]> {
    const { data } = await apiClient.get<PaymentResponseDto[]>(BASE);
    return data;
  },

  async listByCustomer(customerId: number): Promise<PaymentResponseDto[]> {
    const { data } = await apiClient.get<PaymentResponseDto[]>(`${BASE}/by-customer/${customerId}`);
    return data;
  },
};
