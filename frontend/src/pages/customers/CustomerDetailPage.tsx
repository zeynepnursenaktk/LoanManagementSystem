import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Divider,
  Grid,
  LinearProgress,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { customerService } from "@/services";
import { extractErrorMessage } from "@/api/axiosClient";
import { PageHeader } from "@/components/PageHeader";
import { LoanStatusChip } from "@/components/StatusChip";
import { CreditScoreCard } from "@/components/CreditScoreCard";
import { useCreditScore } from "@/hooks/useCreditScore";
import { formatCurrency } from "@/utils/formatters";
import type { CustomerResponseDto, CustomerSummaryDto } from "@/types";

export function CustomerDetailPage(): JSX.Element {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const customerId = Number(id);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [customer, setCustomer] = useState<CustomerResponseDto | null>(null);
  const [summary, setSummary] = useState<CustomerSummaryDto | null>(null);

  const creditScore = useCreditScore(
    Number.isFinite(customerId) && customerId > 0 ? customerId : null,
  );

  useEffect(() => {
    if (!Number.isFinite(customerId) || customerId <= 0) return;
    let cancelled = false;
    const load = async (): Promise<void> => {
      setLoading(true);
      setError(null);
      try {
        const [c, s] = await Promise.all([
          customerService.getById(customerId),
          customerService.getSummary(customerId),
        ]);
        if (cancelled) return;
        setCustomer(c);
        setSummary(s);
      } catch (err) {
        if (!cancelled) setError(extractErrorMessage(err));
      } finally {
        if (!cancelled) setLoading(false);
      }
    };
    void load();
    return () => {
      cancelled = true;
    };
  }, [customerId]);

  if (loading) return <LinearProgress />;
  if (error)
    return (
      <Alert severity="error" action={<Button onClick={() => navigate(-1)}>Geri</Button>}>
        {error}
      </Alert>
    );
  if (!customer || !summary) return <Alert severity="info">Müşteri bulunamadı.</Alert>;

  return (
    <Box>
      <PageHeader
        title={customer.firstName + " " + customer.lastName}
        subtitle={`Müşteri #${customer.id}`}
        actions={
          <Button startIcon={<ArrowBackIcon />} onClick={() => navigate(-1)}>
            Geri
          </Button>
        }
      />

      <Grid container spacing={3}>
        <Grid item xs={12} md={4}>
          <Card sx={{ height: "100%" }}>
            <CardContent>
              <Typography variant="overline" color="text.secondary">
                Profil
              </Typography>
              <Stack spacing={1} sx={{ mt: 1 }}>
                <Typography variant="body2">
                  <strong>E-posta:</strong> {customer.email}
                </Typography>
                <Typography variant="body2">
                  <strong>Telefon:</strong> {customer.phoneNumber ?? "—"}
                </Typography>
                <Typography variant="body2">
                  <strong>T.C.:</strong> {customer.identityNumber}
                </Typography>
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={4}>
          <CreditScoreCard
            loading={creditScore.loading}
            data={creditScore.data}
            error={creditScore.error}
            onRefresh={creditScore.refresh}
          />
        </Grid>

        <Grid item xs={12} md={4}>
          <Card sx={{ height: "100%" }}>
            <CardContent>
              <Typography variant="overline" color="text.secondary">
                Finansal Özet
              </Typography>
              <Grid container spacing={2} sx={{ mt: 0.5 }}>
                <Grid item xs={6}>
                  <Typography variant="caption" color="text.secondary">
                    Toplam Borç
                  </Typography>
                  <Typography variant="h6">{formatCurrency(summary.totalDebt)}</Typography>
                </Grid>
                <Grid item xs={6}>
                  <Typography variant="caption" color="text.secondary">
                    Ödenen
                  </Typography>
                  <Typography variant="h6">{formatCurrency(summary.totalPaid)}</Typography>
                </Grid>
                <Grid item xs={6}>
                  <Typography variant="caption" color="text.secondary">
                    Toplam Kredi
                  </Typography>
                  <Typography variant="h6">{summary.totalLoans}</Typography>
                </Grid>
                <Grid item xs={6}>
                  <Typography variant="caption" color="text.secondary">
                    Gecikmiş Taksit
                  </Typography>
                  <Typography
                    variant="h6"
                    color={summary.overdueInstallments > 0 ? "error" : undefined}
                  >
                    {summary.overdueInstallments}
                  </Typography>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12}>
          <Paper sx={{ p: 2 }}>
            <Typography variant="h6" sx={{ mb: 2 }}>
              Krediler
            </Typography>
            <Divider sx={{ mb: 2 }} />
            {summary.loans.length === 0 ? (
              <Alert severity="info">Henüz kredi kaydı yok.</Alert>
            ) : (
              <Stack spacing={2}>
                {summary.loans.map((loan) => (
                  <Card key={loan.loanId} variant="outlined">
                    <CardContent>
                      <Stack
                        direction={{ xs: "column", sm: "row" }}
                        justifyContent="space-between"
                        alignItems={{ xs: "flex-start", sm: "center" }}
                        spacing={2}
                      >
                        <Box>
                          <Stack direction="row" spacing={1} alignItems="center">
                            <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
                              {loan.loanTypeName}
                            </Typography>
                            <LoanStatusChip status={loan.status} />
                          </Stack>
                          <Typography variant="body2" color="text.secondary">
                            Kredi #{loan.loanId} • Anapara: {formatCurrency(loan.amount)} • Toplam:{" "}
                            {formatCurrency(loan.totalPayable)} • Kalan:{" "}
                            {formatCurrency(loan.remainingDebt)}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {loan.paidInstallments}/{loan.totalInstallments} taksit ödendi
                            {loan.overdueInstallments > 0
                              ? ` • ${loan.overdueInstallments} gecikmiş`
                              : ""}
                          </Typography>
                        </Box>
                        <Button variant="outlined" onClick={() => navigate(`/loans/${loan.loanId}`)}>
                          Detay
                        </Button>
                      </Stack>
                    </CardContent>
                  </Card>
                ))}
              </Stack>
            )}
          </Paper>
        </Grid>
      </Grid>
    </Box>
  );
}
