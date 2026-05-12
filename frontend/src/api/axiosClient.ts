import axios, { AxiosError, type InternalAxiosRequestConfig } from "axios";
import type { ApiErrorBody } from "@/types";

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5213";

export const STORAGE_KEYS = {
  token: "lms.token",
  user: "lms.user",
} as const;

export const apiClient = axios.create({
  baseURL: BASE_URL,
  headers: { "Content-Type": "application/json" },
});

apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = localStorage.getItem(STORAGE_KEYS.token);
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

type ApiError = AxiosError<ApiErrorBody>;

export const extractErrorMessage = (error: unknown): string => {
  const err = error as ApiError;
  const body = err?.response?.data;
  if (body?.errors && body.errors.length > 0) return body.errors.join(" • ");
  if (body?.message) return body.message;
  if (err?.message) return err.message;
  return "Bilinmeyen bir hata oluştu.";
};

let unauthorizedHandler: (() => void) | null = null;
export const registerUnauthorizedHandler = (handler: () => void): void => {
  unauthorizedHandler = handler;
};

apiClient.interceptors.response.use(
  (response) => response,
  (error: ApiError) => {
    if (error.response?.status === 401) {
      localStorage.removeItem(STORAGE_KEYS.token);
      localStorage.removeItem(STORAGE_KEYS.user);
      unauthorizedHandler?.();
    }
    return Promise.reject(error);
  },
);
