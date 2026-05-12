import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Alert,
  Box,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Divider,
  IconButton,
  LinearProgress,
  Skeleton,
  Stack,
  Tooltip,
  Typography,
} from "@mui/material";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import RefreshIcon from "@mui/icons-material/Refresh";
import VerifiedIcon from "@mui/icons-material/Verified";
import TrendingUpIcon from "@mui/icons-material/TrendingUp";
import TrendingDownIcon from "@mui/icons-material/TrendingDown";
import HorizontalRuleIcon from "@mui/icons-material/HorizontalRule";
import {
  CreditRiskLabels,
  type CreditRiskLevel,
  type CreditScoreFactor,
  type CreditScoreResponseDto,
} from "@/types";
import { formatDateTime } from "@/utils/formatters";

interface CreditScoreCardProps {
  loading: boolean;
  data: CreditScoreResponseDto | null;
  error: string | null;
  onRefresh: () => void;
}

const RISK_COLORS: Record<CreditRiskLevel, "success" | "info" | "warning" | "error"> = {
  VeryLow: "success",
  Low: "success",
  Medium: "warning",
  High: "warning",
  VeryHigh: "error",
};

const SCORE_MIN = 300;
const SCORE_MAX = 1900;

function progress(score: number): number {
  const clamped = Math.max(SCORE_MIN, Math.min(SCORE_MAX, score));
  return ((clamped - SCORE_MIN) / (SCORE_MAX - SCORE_MIN)) * 100;
}

export function CreditScoreCard({ loading, data, error, onRefresh }: CreditScoreCardProps): JSX.Element {
  return (
    <Card sx={{ height: "100%" }}>
      <CardContent>
        <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ mb: 1 }}>
          <Typography variant="overline" color="text.secondary">
            Kredi Skoru (Dış Sağlayıcı)
          </Typography>
          <Tooltip title="Skoru yenile">
            <span>
              <IconButton size="small" onClick={onRefresh} disabled={loading} aria-label="kredi skoru yenile">
                {loading ? <CircularProgress size={18} /> : <RefreshIcon fontSize="small" />}
              </IconButton>
            </span>
          </Tooltip>
        </Stack>

        {loading && !data ? (
          <Stack spacing={1.5}>
            <Skeleton variant="text" width="40%" height={42} />
            <Skeleton variant="rounded" height={10} />
            <Skeleton variant="text" width="60%" />
            <Skeleton variant="text" width="80%" />
          </Stack>
        ) : null}

        {error && !loading ? (
          <Alert severity="warning" action={
            <IconButton size="small" onClick={onRefresh} aria-label="yeniden dene">
              <RefreshIcon fontSize="small" />
            </IconButton>
          }>
            {error}
          </Alert>
        ) : null}

        {data && !error ? (
          <Stack spacing={1.5}>
            <Stack direction="row" alignItems="baseline" spacing={1}>
              <Typography variant="h3" sx={{ fontWeight: 700 }}>
                {data.creditScore}
              </Typography>
              <Chip
                size="small"
                label={CreditRiskLabels[data.riskLevel] ?? data.riskLevel}
                color={RISK_COLORS[data.riskLevel] ?? "default"}
              />
              {data.isEligible ? (
                <Tooltip title="Kredi başvurusu için uygun">
                  <VerifiedIcon color="success" fontSize="small" />
                </Tooltip>
              ) : null}
            </Stack>

            <Box>
              <LinearProgress
                variant="determinate"
                value={progress(data.creditScore)}
                color={RISK_COLORS[data.riskLevel] ?? "primary"}
                sx={{ height: 8, borderRadius: 4 }}
              />
              <Stack direction="row" justifyContent="space-between" sx={{ mt: 0.5 }}>
                <Typography variant="caption" color="text.secondary">
                  {SCORE_MIN}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {SCORE_MAX}
                </Typography>
              </Stack>
            </Box>

            <Stack spacing={0.25}>
              <Typography variant="caption" color="text.secondary">
                Sağlayıcı: <strong>{data.providerName}</strong>
              </Typography>
              <Typography variant="caption" color="text.secondary">
                Referans: <code>{data.providerReference}</code>
              </Typography>
              <Typography variant="caption" color="text.secondary">
                Sorgu zamanı: {formatDateTime(data.queriedAtUtc)}
              </Typography>
            </Stack>

            {data.factors && data.factors.length > 0 ? (
              <ScoreFactorsAccordion factors={data.factors} totalScore={data.creditScore} />
            ) : null}
          </Stack>
        ) : null}
      </CardContent>
    </Card>
  );
}

interface ScoreFactorsAccordionProps {
  factors: CreditScoreFactor[];
  totalScore: number;
}

function ScoreFactorsAccordion({ factors, totalScore }: ScoreFactorsAccordionProps): JSX.Element {
  const positives = factors.filter((f) => f.delta > 0).length;
  const negatives = factors.filter((f) => f.delta < 0).length;

  return (
    <Accordion disableGutters elevation={0} sx={{ "&:before": { display: "none" }, mt: 1 }}>
      <AccordionSummary
        expandIcon={<ExpandMoreIcon />}
        sx={{ px: 1, minHeight: 36, "& .MuiAccordionSummary-content": { my: 0.5 } }}
      >
        <Typography variant="caption" sx={{ fontWeight: 600 }}>
          Skor Detayı
        </Typography>
        <Typography variant="caption" color="text.secondary" sx={{ ml: 1 }}>
          ({positives} artırıcı · {negatives} azaltıcı)
        </Typography>
      </AccordionSummary>
      <AccordionDetails sx={{ px: 1, pt: 0 }}>
        <Stack spacing={0.5} divider={<Divider flexItem />}>
          {factors.map((f) => (
            <FactorRow key={f.code} factor={f} />
          ))}
          <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ pt: 0.5 }}>
            <Typography variant="caption" sx={{ fontWeight: 700 }}>
              Toplam
            </Typography>
            <Typography variant="caption" sx={{ fontWeight: 700 }}>
              {totalScore}
            </Typography>
          </Stack>
        </Stack>
      </AccordionDetails>
    </Accordion>
  );
}

function FactorRow({ factor }: { factor: CreditScoreFactor }): JSX.Element {
  const isPositive = factor.delta > 0;
  const isNegative = factor.delta < 0;
  const color = isPositive ? "success.main" : isNegative ? "error.main" : "text.secondary";
  const Icon = isPositive ? TrendingUpIcon : isNegative ? TrendingDownIcon : HorizontalRuleIcon;
  const sign = factor.delta > 0 ? "+" : "";

  return (
    <Stack direction="row" alignItems="center" spacing={1}>
      <Icon sx={{ fontSize: 16, color }} />
      <Typography variant="caption" sx={{ flex: 1 }}>
        {factor.label}
      </Typography>
      <Typography variant="caption" sx={{ color, fontWeight: 600, minWidth: 48, textAlign: "right" }}>
        {sign}
        {factor.delta}
      </Typography>
    </Stack>
  );
}
