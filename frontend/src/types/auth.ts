import type { AppRole } from "./enums";

export interface LoginRequestDto {
  username: string;
  password: string;
}

export interface RegisterRequestDto {
  username: string;
  password: string;
  firstName: string;
  lastName: string;
  identityNumber: string;
  email: string;
  phoneNumber?: string | null;
}

export interface LoginResponseDto {
  token: string;
  username: string;
  fullName: string;
  role: AppRole;
  customerId: number | null;
  expiresAt: string;
}
