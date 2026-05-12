import { useCallback, useEffect, useMemo, useState } from "react";
import { Alert, Box, Paper } from "@mui/material";
import { DataGrid, type GridColDef } from "@mui/x-data-grid";
import { paymentService } from "@/services";
import { extractErrorMessage } from "@/api/axiosClient";
import { PageHeader } from "@/components/PageHeader";
import { formatCurrency, formatDateTime } from "@/utils/formatters";
import { useAuth } from "@/hooks/useAuth";
import type { PaymentResponseDto } from "@/types";

export function PaymentsListPage(): JSX.Element {
  const { isAdmin, user } = useAuth();
  const [rows, setRows] = useState<PaymentResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async (): Promise<void> => {
    setLoading(true);
    setError(null);
    try {
      const data = isAdmin
        ? await paymentService.listAll()
        : user?.customerId
          ? await paymentService.listByCustomer(user.customerId)
          : [];
      setRows(data);
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }, [isAdmin, user?.customerId]);

  useEffect(() => {
    void load();
  }, [load]);

  const columns = useMemo<GridColDef<PaymentResponseDto>[]>(
    () => [
      { field: "paymentId", headerName: "ID", width: 80 },
      {
        field: "paymentDate",
        headerName: "Tarih",
        width: 170,
        valueFormatter: (value) => formatDateTime(value as string),
      },
      ...(isAdmin
        ? [{ field: "customerName", headerName: "Müşteri", flex: 1, minWidth: 160 } as GridColDef<PaymentResponseDto>]
        : []),
      { field: "loanTypeName", headerName: "Kredi Türü", flex: 1, minWidth: 140 },
      { field: "loanId", headerName: "Kredi", width: 90, type: "number" },
      { field: "installmentNumber", headerName: "Taksit #", width: 100, type: "number" },
      {
        field: "paidAmount",
        headerName: "Ödenen",
        flex: 1,
        minWidth: 130,
        type: "number",
        valueFormatter: (value) => formatCurrency(value as number),
      },
      {
        field: "providerName",
        headerName: "Sağlayıcı",
        width: 140,
        renderCell: (params) => (params.value ? <code>{params.value}</code> : <span>—</span>),
      },
      {
        field: "providerReference",
        headerName: "İşlem No",
        width: 220,
        renderCell: (params) =>
          params.value ? (
            <code style={{ fontSize: "0.75rem" }}>{params.value}</code>
          ) : (
            <span>—</span>
          ),
      },
    ],
    [isAdmin],
  );

  return (
    <Box>
      <PageHeader title="Ödeme Geçmişi" subtitle={isAdmin ? "Tüm ödeme kayıtları" : "Kendi ödemelerim"} />

      {error ? (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      ) : null}

      <Paper sx={{ height: 600, width: "100%" }}>
        <DataGrid
          rows={rows}
          columns={columns}
          loading={loading}
          getRowId={(row) => row.paymentId}
          disableRowSelectionOnClick
          initialState={{ pagination: { paginationModel: { pageSize: 10, page: 0 } } }}
          pageSizeOptions={[10, 25, 50]}
        />
      </Paper>
    </Box>
  );
}
