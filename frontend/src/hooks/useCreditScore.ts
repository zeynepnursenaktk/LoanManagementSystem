import { useCallback, useEffect, useState } from "react";
import { AxiosError } from "axios";
import { creditScoreService } from "@/services";
import { extractErrorMessage } from "@/api/axiosClient";
import type { CreditScoreProviderError, CreditScoreResponseDto } from "@/types";

interface UseCreditScoreResult {
  data: CreditScoreResponseDto | null;
  loading: boolean;
  error: string | null;
  refresh: () => void;
}

export function useCreditScore(customerId: number | null | undefined): UseCreditScoreResult {
  const [data, setData] = useState<CreditScoreResponseDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async (): Promise<void> => {
    if (!customerId || customerId <= 0) {
      setData(null);
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const res = await creditScoreService.getForCustomer(customerId);
      setData(res);
    } catch (err) {
      const ax = err as AxiosError<CreditScoreProviderError>;
      if (ax?.response?.status === 503 && ax.response.data?.providerError) {
        setError(ax.response.data.message ?? "Kredi skoru sağlayıcısına ulaşılamadı.");
      } else {
        setError(extractErrorMessage(err));
      }
      setData(null);
    } finally {
      setLoading(false);
    }
  }, [customerId]);

  useEffect(() => {
    void load();
  }, [load]);

  return { data, loading, error, refresh: () => void load() };
}
