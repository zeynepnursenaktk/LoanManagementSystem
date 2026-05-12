import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Alert, Box, Button, Chip, IconButton, Paper, Stack, Tooltip } from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import RestoreIcon from "@mui/icons-material/Restore";
import VisibilityIcon from "@mui/icons-material/Visibility";
import { DataGrid, type GridColDef } from "@mui/x-data-grid";
import { toast } from "react-toastify";
import { customerService } from "@/services";
import { extractErrorMessage } from "@/api/axiosClient";
import { PageHeader } from "@/components/PageHeader";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { CustomerFormDialog } from "./CustomerFormDialog";
import type { CustomerListDto } from "@/types";

type ConfirmMode = "delete" | "restore";

export function CustomersListPage(): JSX.Element {
  const navigate = useNavigate();
  const [rows, setRows] = useState<CustomerListDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [formOpen, setFormOpen] = useState(false);
  const [editTarget, setEditTarget] = useState<CustomerListDto | null>(null);

  const [confirmOpen, setConfirmOpen] = useState(false);
  const [confirmMode, setConfirmMode] = useState<ConfirmMode>("delete");
  const [confirmTarget, setConfirmTarget] = useState<CustomerListDto | null>(null);
  const [confirmBusy, setConfirmBusy] = useState(false);

  const load = useCallback(async (): Promise<void> => {
    setLoading(true);
    setError(null);
    try {
      // Admin müşteri yönetimi: aktif + pasif (soft-delete edilmiş) tüm müşteriler.
      const data = await customerService.list(true);
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

  const openCreate = (): void => {
    setEditTarget(null);
    setFormOpen(true);
  };

  const openEdit = (row: CustomerListDto): void => {
    setEditTarget(row);
    setFormOpen(true);
  };

  const openDelete = (row: CustomerListDto): void => {
    setConfirmMode("delete");
    setConfirmTarget(row);
    setConfirmOpen(true);
  };

  const openRestore = (row: CustomerListDto): void => {
    setConfirmMode("restore");
    setConfirmTarget(row);
    setConfirmOpen(true);
  };

  const closeConfirm = (): void => {
    setConfirmOpen(false);
    setConfirmTarget(null);
  };

  const handleConfirm = async (): Promise<void> => {
    if (!confirmTarget) return;
    setConfirmBusy(true);
    try {
      const res =
        confirmMode === "delete"
          ? await customerService.remove(confirmTarget.id)
          : await customerService.restore(confirmTarget.id);
      toast.success(
        res.message ??
          (confirmMode === "delete"
            ? "Müşteri silindi."
            : "Müşteri yeniden aktifleştirildi."),
      );
      closeConfirm();
      await load();
    } catch (err) {
      toast.error(extractErrorMessage(err));
    } finally {
      setConfirmBusy(false);
    }
  };

  const formatDeletedAt = (iso: string | null): string => {
    if (!iso) return "";
    const d = new Date(iso);
    if (Number.isNaN(d.getTime())) return "";
    return d.toLocaleString("tr-TR", { dateStyle: "short", timeStyle: "short" });
  };

  const columns = useMemo<GridColDef<CustomerListDto>[]>(
    () => [
      { field: "id", headerName: "ID", width: 80 },
      { field: "firstName", headerName: "Ad", flex: 1, minWidth: 120 },
      { field: "lastName", headerName: "Soyad", flex: 1, minWidth: 120 },
      { field: "email", headerName: "E-posta", flex: 1.5, minWidth: 200 },
      { field: "phoneNumber", headerName: "Telefon", flex: 1, minWidth: 140 },
      { field: "totalLoans", headerName: "Kredi Sayısı", width: 120, type: "number" },
      {
        field: "status",
        headerName: "Durum",
        width: 130,
        sortable: false,
        filterable: false,
        valueGetter: (_value, row) => (row.isDeleted ? "Pasif" : "Aktif"),
        renderCell: (params) => {
          const row = params.row;
          if (row.isDeleted) {
            const tip = row.deletedAtUtc
              ? `Silinme: ${formatDeletedAt(row.deletedAtUtc)}`
              : "Soft-delete edilmiş müşteri";
            return (
              <Tooltip title={tip}>
                <Chip label="Pasif" color="default" size="small" variant="outlined" />
              </Tooltip>
            );
          }
          return <Chip label="Aktif" color="success" size="small" />;
        },
      },
      {
        field: "actions",
        headerName: "İşlemler",
        width: 170,
        sortable: false,
        filterable: false,
        renderCell: (params) => {
          const row = params.row;
          const isDeleted = row.isDeleted;
          return (
            <Stack direction="row" spacing={0.5}>
              <Tooltip title="Detay">
                <IconButton size="small" onClick={() => navigate(`/customers/${row.id}`)}>
                  <VisibilityIcon fontSize="small" />
                </IconButton>
              </Tooltip>
              {!isDeleted && (
                <Tooltip title="Güncelle">
                  <IconButton size="small" onClick={() => openEdit(row)}>
                    <EditIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
              )}
              {isDeleted ? (
                <Tooltip title="Aktife Çevir">
                  <IconButton
                    size="small"
                    color="success"
                    onClick={() => openRestore(row)}
                    aria-label={`Aktife çevir: ${row.firstName} ${row.lastName}`}
                  >
                    <RestoreIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
              ) : (
                <Tooltip title="Sil">
                  <IconButton
                    size="small"
                    color="error"
                    onClick={() => openDelete(row)}
                    aria-label={`Sil: ${row.firstName} ${row.lastName}`}
                  >
                    <DeleteIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
              )}
            </Stack>
          );
        },
      },
    ],
    [navigate],
  );

  const confirmTitle =
    confirmMode === "delete" ? "Müşteriyi sil" : "Müşteriyi aktife çevir";
  const confirmDescription =
    confirmMode === "delete"
      ? `${confirmTarget?.firstName ?? ""} ${
          confirmTarget?.lastName ?? ""
        } adlı müşteriyi silmek istediğinizden emin misiniz? Aktif kredisi olan müşteriler silinemez.`
      : `${confirmTarget?.firstName ?? ""} ${
          confirmTarget?.lastName ?? ""
        } adlı müşteriyi yeniden aktifleştirmek istiyor musunuz? Kredi geçmişi korunarak müşteri tekrar erişilebilir hale gelecektir.`;
  const confirmLabel = confirmMode === "delete" ? "Sil" : "Aktife Çevir";

  return (
    <Box>
      <PageHeader
        title="Müşteriler"
        subtitle="Sistemdeki tüm müşterileri yönetin (aktif + pasif)"
        actions={
          <Button variant="contained" startIcon={<AddIcon />} onClick={openCreate}>
            Yeni Müşteri
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
          initialState={{
            pagination: { paginationModel: { pageSize: 10, page: 0 } },
          }}
          pageSizeOptions={[10, 25, 50]}
          disableRowSelectionOnClick
          sx={{
            // Pasif satırları görsel olarak biraz soluklaştır (UX ipucu)
            "& .row-deleted": {
              opacity: 0.72,
              fontStyle: "italic",
            },
          }}
          getRowClassName={(params) =>
            params.row.isDeleted ? "row-deleted" : ""
          }
        />
      </Paper>

      <CustomerFormDialog
        open={formOpen}
        target={editTarget}
        onClose={() => setFormOpen(false)}
        onSuccess={async () => {
          setFormOpen(false);
          await load();
        }}
      />

      <ConfirmDialog
        open={confirmOpen}
        title={confirmTitle}
        description={confirmDescription}
        confirmLabel={confirmLabel}
        destructive={confirmMode === "delete"}
        loading={confirmBusy}
        onCancel={closeConfirm}
        onConfirm={handleConfirm}
      />
    </Box>
  );
}
