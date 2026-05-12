import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Alert,
  Box,
  Button,
  IconButton,
  Paper,
  Stack,
  Tooltip,
} from "@mui/material";
import RefreshIcon from "@mui/icons-material/Refresh";
import UpdateIcon from "@mui/icons-material/Update";
import VisibilityIcon from "@mui/icons-material/Visibility";
import { DataGrid, type GridColDef } from "@mui/x-data-grid";
import { toast } from "react-toastify";
import { installmentService, loanService } from "@/services";
import { extractErrorMessage } from "@/api/axiosClient";
import { PageHeader } from "@/components/PageHeader";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { formatCurrency, formatDate } from "@/utils/formatters";
import { useAuth } from "@/hooks/useAuth";
import type { InstallmentDto, UnpaidInstallmentDto } from "@/types";

type Row = (UnpaidInstallmentDto | InstallmentDto) & { customerLabel?: string };

export function OverdueInstallmentsPage(): JSX.Element {
  const navigate = useNavigate();
  const { isAdmin, user } = useAuth();
  const [rows, setRows] = useState<Row[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async (): Promise<void> => {
    setLoading(true);
    setError(null);
    try {
      if (isAdmin) {
        const loans = await loanService.list();
        const overdue: Row[] = loans.flatMap((l) =>
          l.installments
            .filter((i) => i.status === "Overdue")
            .map((i) => ({ ...i, customerLabel: l.customerFullName })),
        );
        setRows(overdue);
      } else if (user?.customerId) {
        const data = await installmentService.overdueByCustomer(user.customerId);
        setRows(data);
      }
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }, [isAdmin, user?.customerId]);

  useEffect(() => {
    void load();
  }, [load]);

  const handleRefreshOverdue = async (): Promise<void> => {
    setRefreshing(true);
    try {
      const res = await installmentService.refreshOverdue();
      toast.success(res.message ?? `${res.updatedCount} taksit güncellendi.`);
      setConfirmOpen(false);
      await load();
    } catch (err) {
      toast.error(extractErrorMessage(err));
    } finally {
      setRefreshing(false);
    }
  };

  const columns = useMemo<GridColDef<Row>[]>(
    () => [
      { field: "id", headerName: "ID", width: 80 },
      ...(isAdmin
        ? [{ field: "customerLabel", headerName: "Müşteri", flex: 1, minWidth: 160 } as GridColDef<Row>]
        : []),
      { field: "loanId", headerName: "Kredi #", width: 100, type: "number" },
      { field: "installmentNumber", headerName: "Taksit #", width: 110, type: "number" },
      {
        field: "amount",
        headerName: "Tutar",
        flex: 1,
        minWidth: 130,
        type: "number",
        valueFormatter: (value) => formatCurrency(value as number),
      },
      {
        field: "dueDate",
        headerName: "Vade",
        width: 130,
        valueFormatter: (value) => formatDate(value as string),
      },
      {
        field: "actions",
        headerName: "İşlemler",
        width: 110,
        sortable: false,
        filterable: false,
        renderCell: (params) => (
          <Tooltip title="Krediye git">
            <IconButton size="small" onClick={() => navigate(`/loans/${params.row.loanId}`)}>
              <VisibilityIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        ),
      },
    ],
    [isAdmin, navigate],
  );

  return (
    <Box>
      <PageHeader
        title="Gecikmiş Taksitler"
        subtitle={isAdmin ? "Tüm gecikmiş taksitler" : "Gecikmiş taksitleriniz"}
        actions={
          <Stack direction="row" spacing={1}>
            <Button startIcon={<RefreshIcon />} onClick={() => void load()}>
              Yenile
            </Button>
            {isAdmin ? (
              <Button
                variant="contained"
                color="warning"
                startIcon={<UpdateIcon />}
                onClick={() => setConfirmOpen(true)}
              >
                Vadesi Geçenleri Güncelle
              </Button>
            ) : null}
          </Stack>
        }
      />

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
          getRowId={(row) => row.id}
          disableRowSelectionOnClick
          initialState={{ pagination: { paginationModel: { pageSize: 10, page: 0 } } }}
          pageSizeOptions={[10, 25, 50]}
        />
      </Paper>

      <ConfirmDialog
        open={confirmOpen}
        title="Gecikmiş Taksitleri Güncelle"
        description="Vadesi geçmiş tüm ödenmemiş taksitler 'Gecikmiş' olarak işaretlenecek. Devam etmek istiyor musunuz?"
        confirmLabel="Güncelle"
        loading={refreshing}
        onCancel={() => setConfirmOpen(false)}
        onConfirm={handleRefreshOverdue}
      />
    </Box>
  );
}
