import { createContext, useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import { STORAGE_KEYS, registerUnauthorizedHandler } from "@/api/axiosClient";
import { authService } from "@/services";
import type { AppRole, LoginRequestDto, LoginResponseDto, RegisterRequestDto } from "@/types";

export interface AuthUser {
  username: string;
  fullName: string;
  role: AppRole;
  customerId: number | null;
  expiresAt: string;
}

export interface AuthContextValue {
  user: AuthUser | null;
  token: string | null;
  isAuthenticated: boolean;
  isAdmin: boolean;
  isCustomer: boolean;
  loading: boolean;
  login: (payload: LoginRequestDto) => Promise<LoginResponseDto>;
  register: (payload: RegisterRequestDto) => Promise<LoginResponseDto>;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);

const loadUserFromStorage = (): AuthUser | null => {
  const raw = localStorage.getItem(STORAGE_KEYS.user);
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw) as AuthUser;
    if (parsed.expiresAt && new Date(parsed.expiresAt).getTime() < Date.now()) {
      localStorage.removeItem(STORAGE_KEYS.user);
      localStorage.removeItem(STORAGE_KEYS.token);
      return null;
    }
    return parsed;
  } catch {
    return null;
  }
};

interface AuthProviderProps {
  children: ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps): JSX.Element {
  const [user, setUser] = useState<AuthUser | null>(() => loadUserFromStorage());
  const [token, setToken] = useState<string | null>(() => localStorage.getItem(STORAGE_KEYS.token));
  const [loading, setLoading] = useState(false);

  const handleAuthResponse = useCallback((res: LoginResponseDto): LoginResponseDto => {
    const next: AuthUser = {
      username: res.username,
      fullName: res.fullName,
      role: res.role,
      customerId: res.customerId,
      expiresAt: res.expiresAt,
    };
    localStorage.setItem(STORAGE_KEYS.token, res.token);
    localStorage.setItem(STORAGE_KEYS.user, JSON.stringify(next));
    setToken(res.token);
    setUser(next);
    return res;
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem(STORAGE_KEYS.token);
    localStorage.removeItem(STORAGE_KEYS.user);
    setToken(null);
    setUser(null);
  }, []);

  useEffect(() => {
    registerUnauthorizedHandler(() => {
      setToken(null);
      setUser(null);
    });
  }, []);

  const login = useCallback(
    async (payload: LoginRequestDto): Promise<LoginResponseDto> => {
      setLoading(true);
      try {
        const res = await authService.login(payload);
        return handleAuthResponse(res);
      } finally {
        setLoading(false);
      }
    },
    [handleAuthResponse],
  );

  const register = useCallback(
    async (payload: RegisterRequestDto): Promise<LoginResponseDto> => {
      setLoading(true);
      try {
        const res = await authService.register(payload);
        return handleAuthResponse(res);
      } finally {
        setLoading(false);
      }
    },
    [handleAuthResponse],
  );

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      token,
      isAuthenticated: !!token && !!user,
      isAdmin: user?.role === "Admin",
      isCustomer: user?.role === "Customer",
      loading,
      login,
      register,
      logout,
    }),
    [user, token, loading, login, register, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
