import { useEffect, useState } from "react";
import {
  Alert,
  Box,
  Card,
  CardContent,
  Grid,
  LinearProgress,
  Stack,
  Typography,
} from "@mui/material";
import PaymentsIcon from "@mui/icons-material/Payments";
import RequestQuoteIcon from "@mui/icons-material/RequestQuote";
import EventBusyIcon from "@mui/icons-material/EventBusy";
import GroupIcon from "@mui/icons-material/Group";
import { useAuth } from "@/hooks/useAuth";
import { customerService, loanService, installmentService } from "@/services";
import { extractErrorMessage } from "@/api/axiosClient";
import { PageHeader } from "@/components/PageHeader";
import { formatCurrency } from "@/utils/formatters";
import type { CustomerSummaryDto, LoanResponseDto, UnpaidInstallmentDto } from "@/types";

interface StatCardProps {
  label: string;
  value: string;
  icon: JSX.Element;
  accent: string;
}

function StatCard({ label, value, icon, accent }: StatCardProps): JSX.Element {
  return (
    <Card sx={{ height: "100%", borderLeft: `4px solid ${accent}` }}>
      <CardContent>
        <Stack direction="row" alignItems="center" spacing={2}>
          <Box
            sx={{
              width: 48,
              height: 48,
              borderRadius: 2,
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              backgroundColor: `${accent}1a`,
              color: accent,
            }}
          >
            {icon}
          </Box>
          <Box>
            <Typography variant="caption" color="text.secondary" sx={{ textTransform: "uppercase", letterSpacing: 0.5 }}>
              {label}
            </Typography>
            <Typography variant="h6" sx={{ fontWeight: 700 }}>
              {value}
            </Typography>
          </Box>
        </Stack>
      </CardContent>
    </Card>
  );
}

export function DashboardPage(): JSX.Element {
  const { user, isAdmin, isCustomer } = useAuth();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [summary, setSummary] = useState<CustomerSummaryDto | null>(null);
  const [adminLoans, setAdminLoans] = useState<LoanResponseDto[] | null>(null);
  const [overdue, setOverdue] = useState<UnpaidInstallmentDto[]>([]);

  useEffect(() => {
    let cancelled = false;
    const load = async (): Promise<void> => {
      setLoading(true);
      setError(null);
      try {
        if (isAdmin) {
          const loans = await loanService.list();
          if (!cancelled) setAdminLoans(loans);
        } else if (isCustomer && user?.customerId) {
          const [s, ov] = await Promise.all([
            customerService.getSummary(user.customerId),
            installmentService.overdueByCustomer(user.customerId),
          ]);
          if (!cancelled) {
            setSummary(s);
            setOverdue(ov);
          }
        }
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
  }, [isAdmin, isCustomer, user?.customerId]);

  if (loading) return <LinearProgress />;
  if (error) return <Alert severity="error">{error}</Alert>;

  return (
    <Box>
      <PageHeader
        title={`Hoş geldiniz, ${user?.fullName ?? ""}`}
        subtitle={isAdmin ? "Sistem geneli özet" : "Kişisel kredi panonuz"}
      />

      {isAdmin && adminLoans ? (
        <Grid container spacing={3}>
          <Grid item xs={12} sm={6} md={3}>
            <StatCard
              label="Toplam Kredi"
              value={adminLoans.length.toString()}
              accent="#1a4480"
              icon={<RequestQuoteIcon />}
            />
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <StatCard
              label="Aktif Kredi"
              value={adminLoans.filter((l) => l.status === "Active").length.toString()}
              accent="#0f766e"
              icon={<RequestQuoteIcon />}
            />
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <StatCard
              label="Toplam Anapara"
              value={formatCurrency(adminLoans.reduce((acc, l) => acc + l.amount, 0))}
              accent="#15803d"
              icon={<PaymentsIcon />}
            />
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <StatCard
              label="Toplam Müşteri (kredisi olan)"
              value={new Set(adminLoans.map((l) => l.customerId)).size.toString()}
              accent="#d97706"
              icon={<GroupIcon />}
            />
          </Grid>
        </Grid>
      ) : null}

      {isCustomer && summary ? (
        <>
          <Grid container spacing={3}>
            <Grid item xs={12} sm={6} md={3}>
              <StatCard
                label="Toplam Kredi"
                value={summary.totalLoans.toString()}
                accent="#1a4480"
                icon={<RequestQuoteIcon />}
              />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <StatCard
                label="Aktif Kredi"
                value={summary.activeLoans.toString()}
                accent="#0f766e"
                icon={<RequestQuoteIcon />}
              />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <StatCard
                label="Kalan Borç"
                value={formatCurrency(summary.totalDebt)}
                accent="#b91c1c"
                icon={<PaymentsIcon />}
              />
            </Grid>
            <Grid item xs={12} sm={6} md={3}>
              <StatCard
                label="Gecikmiş Taksit"
                value={summary.overdueInstallments.toString()}
                accent="#d97706"
                icon={<EventBusyIcon />}
              />
            </Grid>
          </Grid>

          {overdue.length > 0 ? (
            <Alert severity="warning" sx={{ mt: 3 }}>
              {overdue.length} adet gecikmiş taksitiniz bulunuyor. Ödeme yapmak için "Krediler" sekmesini kullanın.
            </Alert>
          ) : null}
        </>
      ) : null}
    </Box>
  );
}
