import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Alert, Box, Button, IconButton, Paper, Stack, Tooltip } from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import VisibilityIcon from "@mui/icons-material/Visibility";
import { DataGrid, type GridColDef } from "@mui/x-data-grid";
import { loanService } from "@/services";
import { extractErrorMessage } from "@/api/axiosClient";
import { PageHeader } from "@/components/PageHeader";
import { LoanStatusChip } from "@/components/StatusChip";
import { formatCurrency, formatDate, formatPercent } from "@/utils/formatters";
import { LoanCreateDialog } from "./LoanCreateDialog";
import { useAuth } from "@/hooks/useAuth";
import type { LoanResponseDto } from "@/types";

export function LoansListPage(): JSX.Element {
  const navigate = useNavigate();
  const { isAdmin } = useAuth();
  const [rows, setRows] = useState<LoanResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);

  const load = useCallback(async (): Promise<void> => {
    setLoading(true);
    setError(null);
    try {
      const data = await loanService.list();
      setRows(data);
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const columns = useMemo<GridColDef<LoanResponseDto>[]>(
    () => [
      { field: "id", headerName: "ID", width: 80 },
      ...(isAdmin
        ? [
            { field: "customerFullName", headerName: "Müşteri", flex: 1, minWidth: 160 } as GridColDef<LoanResponseDto>,
          ]
        : []),
      { field: "loanTypeName", headerName: "Tür", flex: 1, minWidth: 130 },
      {
        field: "amount",
        headerName: "Anapara",
        flex: 1,
        minWidth: 130,
        type: "number",
        valueFormatter: (value) => formatCurrency(value as number),
      },
      {
        field: "tenor",
        headerName: "Vade (ay)",
        width: 100,
        type: "number",
      },
      {
        field: "profitRate",
        headerName: "Kar Oranı",
        width: 120,
        type: "number",
        valueFormatter: (value) => formatPercent(value as number),
      },
      {
        field: "totalPayable",
        headerName: "Toplam",
        flex: 1,
        minWidth: 140,
        type: "number",
        valueFormatter: (value) => formatCurrency(value as number),
      },
      {
        field: "startDate",
        headerName: "Başlangıç",
        width: 130,
        valueFormatter: (value) => formatDate(value as string),
      },
      {
        field: "status",
        headerName: "Durum",
        width: 120,
        renderCell: (params) => <LoanStatusChip status={params.value} />,
      },
      {
        field: "actions",
        headerName: "İşlemler",
        width: 110,
        sortable: false,
        filterable: false,
        renderCell: (params) => (
          <Tooltip title="Detay & Taksitler">
            <IconButton size="small" onClick={() => navigate(`/loans/${params.row.id}`)}>
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
        title="Krediler"
        subtitle={isAdmin ? "Tüm kredi başvuruları ve durumları" : "Kredilerim"}
        actions={
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
            Yeni Kredi Başvurusu
          </Button>
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
          initialState={{ pagination: { paginationModel: { pageSize: 10, page: 0 } } }}
          pageSizeOptions={[10, 25, 50]}
          disableRowSelectionOnClick
        />
      </Paper>

      <LoanCreateDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        onSuccess={async () => {
          setCreateOpen(false);
          await load();
        }}
      />

      <Stack direction="row" spacing={1} sx={{ mt: 2 }} useFlexGap flexWrap="wrap">
        <Button variant="text" onClick={() => navigate("/payments")}>
          Ödeme yapma sayfasına git
        </Button>
      </Stack>
    </Box>
  );
}
