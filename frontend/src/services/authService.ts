import { apiClient } from "@/api/axiosClient";
import type { LoginRequestDto, LoginResponseDto, RegisterRequestDto } from "@/types";

const BASE = "/api/Auth";

export const authService = {
  async login(payload: LoginRequestDto): Promise<LoginResponseDto> {
    const { data } = await apiClient.post<LoginResponseDto>(`${BASE}/login`, payload);
    return data;
  },

  async register(payload: RegisterRequestDto): Promise<LoginResponseDto> {
    const { data } = await apiClient.post<LoginResponseDto>(`${BASE}/register`, payload);
    return data;
  },
};
