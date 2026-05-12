import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Grid,
  LinearProgress,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import PaymentIcon from "@mui/icons-material/Payment";
import { DataGrid, type GridColDef } from "@mui/x-data-grid";
import { toast } from "react-toastify";
import { loanService, paymentService } from "@/services";
import { extractErrorMessage } from "@/api/axiosClient";
import { PageHeader } from "@/components/PageHeader";
import { InstallmentStatusChip, LoanStatusChip } from "@/components/StatusChip";
import {
  PaymentProcessingDialog,
  type PaymentProcessingPhase,
} from "@/components/PaymentProcessingDialog";
import { formatCurrency, formatDate, formatPercent } from "@/utils/formatters";
import { parsePaymentError, type ParsedPaymentError } from "@/utils/paymentErrors";
import type { InstallmentDto, LoanResponseDto, PaymentResponseDto } from "@/types";

export function LoanDetailPage(): JSX.Element {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const loanId = Number(id);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [loan, setLoan] = useState<LoanResponseDto | null>(null);

  const [payOpen, setPayOpen] = useState(false);
  const [payPhase, setPayPhase] = useState<PaymentProcessingPhase>("confirm");
  const [payResult, setPayResult] = useState<PaymentResponseDto | null>(null);
  const [payError, setPayError] = useState<ParsedPaymentError | null>(null);

  const load = useCallback(async (): Promise<void> => {
    if (!Number.isFinite(loanId) || loanId <= 0) return;
    setLoading(true);
    setError(null);
    try {
      const data = await loanService.getById(loanId);
      setLoan(data);
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }, [loanId]);

  useEffect(() => {
    void load();
  }, [load]);

  const nextInstallment = useMemo(() => {
    if (!loan) return null;
    return (
      [...loan.installments]
        .sort((a, b) => a.installmentNumber - b.installmentNumber)
        .find((i) => i.status !== "Paid") ?? null
    );
  }, [loan]);

  const openPayDialog = (): void => {
    setPayResult(null);
    setPayError(null);
    setPayPhase("confirm");
    setPayOpen(true);
  };

  const closePayDialog = (): void => {
    if (payPhase === "processing") return;
    setPayOpen(false);
    setPayResult(null);
    setPayError(null);
    setPayPhase("confirm");
  };

  const executePayment = useCallback(async (): Promise<void> => {
    if (!loan) return;
    setPayPhase("processing");
    setPayError(null);
    try {
      const res = await paymentService.pay({ loanId: loan.id });
      setPayResult(res);
      setPayPhase("success");
      toast.success(
        `Taksit #${res.installmentNumber} (${formatCurrency(res.paidAmount)}) başarıyla ödendi.`,
      );
      await load();
    } catch (err) {
      const parsed = parsePaymentError(err);
      setPayError(parsed);
      setPayPhase("error");
      toast.error(parsed.userMessage);
    }
  }, [loan, load]);

  const columns = useMemo<GridColDef<InstallmentDto>[]>(
    () => [
      { field: "installmentNumber", headerName: "Taksit #", width: 100, type: "number" },
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
        field: "status",
        headerName: "Durum",
        width: 130,
        renderCell: (params) => <InstallmentStatusChip status={params.value} />,
      },
      {
        field: "paidAmount",
        headerName: "Ödenen",
        flex: 1,
        minWidth: 130,
        type: "number",
        valueFormatter: (value) => formatCurrency(value as number | null),
      },
      {
        field: "paymentDate",
        headerName: "Ödeme Tarihi",
        width: 160,
        valueFormatter: (value) => formatDate(value as string | null),
      },
    ],
    [],
  );

  if (loading) return <LinearProgress />;
  if (error)
    return (
      <Alert severity="error" action={<Button onClick={() => navigate(-1)}>Geri</Button>}>
        {error}
      </Alert>
    );
  if (!loan) return <Alert severity="info">Kredi bulunamadı.</Alert>;

  return (
    <Box>
      <PageHeader
        title={`${loan.loanTypeName} • #${loan.id}`}
        subtitle={`Müşteri: ${loan.customerFullName}`}
        actions={
          <Stack direction="row" spacing={1}>
            <Button startIcon={<ArrowBackIcon />} onClick={() => navigate(-1)}>
              Geri
            </Button>
            <Button
              variant="contained"
              startIcon={<PaymentIcon />}
              disabled={loan.status === "Closed" || !nextInstallment}
              onClick={openPayDialog}
            >
              Sıradaki Taksiti Öde
            </Button>
          </Stack>
        }
      />

      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12} md={3}>
          <Card>
            <CardContent>
              <Typography variant="caption" color="text.secondary">
                Durum
              </Typography>
              <Box sx={{ mt: 1 }}>
                <LoanStatusChip status={loan.status} />
              </Box>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent>
              <Typography variant="caption" color="text.secondary">
                Anapara
              </Typography>
              <Typography variant="h6">{formatCurrency(loan.amount)}</Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent>
              <Typography variant="caption" color="text.secondary">
                Toplam Geri Ödeme
              </Typography>
              <Typography variant="h6">{formatCurrency(loan.totalPayable)}</Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent>
              <Typography variant="caption" color="text.secondary">
                Yıllık Kar
              </Typography>
              <Typography variant="h6">{formatPercent(loan.profitRate)}</Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent>
              <Typography variant="caption" color="text.secondary">
                Vade
              </Typography>
              <Typography variant="h6">{loan.tenor} ay</Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={6} md={3}>
          <Card>
            <CardContent>
              <Typography variant="caption" color="text.secondary">
                Başlangıç
              </Typography>
              <Typography variant="h6">{formatDate(loan.startDate)}</Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Paper sx={{ p: 2 }}>
        <Typography variant="h6" sx={{ mb: 2 }}>
          Taksit Planı
        </Typography>
        <Box sx={{ height: 500 }}>
          <DataGrid
            rows={loan.installments}
            columns={columns}
            getRowId={(r) => r.id}
            disableRowSelectionOnClick
            initialState={{ pagination: { paginationModel: { pageSize: 12, page: 0 } } }}
            pageSizeOptions={[12, 24, 48]}
          />
        </Box>
      </Paper>

      <PaymentProcessingDialog
        open={payOpen}
        phase={payPhase}
        installmentNumber={nextInstallment?.installmentNumber ?? null}
        amount={nextInstallment?.amount ?? null}
        result={payResult}
        error={payError}
        onCancel={closePayDialog}
        onConfirm={() => void executePayment()}
        onRetry={() => void executePayment()}
        onClose={closePayDialog}
      />
    </Box>
  );
}
