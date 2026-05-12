export * from "./enums";
export * from "./auth";
export * from "./customer";
export * from "./loan";
export * from "./installment";
export * from "./payment";
export * from "./creditScore";

export interface ApiErrorBody {
  message?: string;
  errors?: string[];
  details?: Record<string, string[]> | { error?: string } | unknown;
  statusCode?: number;
}
