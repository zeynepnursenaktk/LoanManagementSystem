import {
  Alert,
  AlertTitle,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  LinearProgress,
  Stack,
  Step,
  StepLabel,
  Stepper,
  Typography,
} from "@mui/material";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import ErrorOutlineIcon from "@mui/icons-material/ErrorOutline";
import type { ParsedPaymentError } from "@/utils/paymentErrors";
import type { PaymentResponseDto } from "@/types";
import { formatCurrency, formatDateTime } from "@/utils/formatters";

export type PaymentProcessingPhase = "confirm" | "processing" | "success" | "error";

interface PaymentProcessingDialogProps {
  open: boolean;
  phase: PaymentProcessingPhase;
  installmentNumber: number | null;
  amount: number | null;
  result: PaymentResponseDto | null;
  error: ParsedPaymentError | null;
  /** İlerleme adımı (görsel) — processing aşamasında animasyonlu */
  onCancel: () => void;
  onConfirm: () => void;
  onRetry: () => void;
  onClose: () => void;
}

const STEPS = ["Onay", "Sağlayıcıya İletim", "Sonuç"];

export function PaymentProcessingDialog({
  open,
  phase,
  installmentNumber,
  amount,
  result,
  error,
  onCancel,
  onConfirm,
  onRetry,
  onClose,
}: PaymentProcessingDialogProps): JSX.Element {
  const activeStep = phase === "confirm" ? 0 : phase === "processing" ? 1 : 2;
  const lockClose = phase === "processing";

  return (
    <Dialog
      open={open}
      onClose={lockClose ? undefined : onClose}
      fullWidth
      maxWidth="sm"
      disableEscapeKeyDown={lockClose}
    >
      <DialogTitle>
        {phase === "confirm" && "Ödeme Onayı"}
        {phase === "processing" && "Ödeme İşleniyor"}
        {phase === "success" && "Ödeme Başarılı"}
        {phase === "error" && "Ödeme Tamamlanamadı"}
      </DialogTitle>

      <DialogContent dividers>
        <Stepper activeStep={activeStep} alternativeLabel sx={{ mb: 3 }}>
          {STEPS.map((label) => (
            <Step key={label}>
              <StepLabel>{label}</StepLabel>
            </Step>
          ))}
        </Stepper>

        {phase === "confirm" && installmentNumber !== null && amount !== null ? (
          <Stack spacing={2}>
            <Alert severity="info">
              Sıradaki taksitiniz dış ödeme sağlayıcısına iletilecek. İşlem birkaç saniye sürebilir.
            </Alert>
            <Box>
              <Typography variant="body2" color="text.secondary">
                Taksit
              </Typography>
              <Typography variant="h6">#{installmentNumber}</Typography>
            </Box>
            <Box>
              <Typography variant="body2" color="text.secondary">
                Tutar
              </Typography>
              <Typography variant="h5" sx={{ fontWeight: 700 }}>
                {formatCurrency(amount)}
              </Typography>
            </Box>
          </Stack>
        ) : null}

        {phase === "processing" ? (
          <Stack spacing={3} alignItems="center" sx={{ py: 3 }}>
            <CircularProgress size={56} thickness={4} />
            <Box textAlign="center">
              <Typography variant="h6">Ödeme sağlayıcısına iletildi</Typography>
              <Typography variant="body2" color="text.secondary">
                Banka onayı bekleniyor. Lütfen sayfayı kapatmayın.
              </Typography>
            </Box>
            <Box sx={{ width: "100%" }}>
              <LinearProgress />
            </Box>
          </Stack>
        ) : null}

        {phase === "success" && result ? (
          <Stack spacing={2} alignItems="center" sx={{ py: 1 }}>
            <CheckCircleIcon color="success" sx={{ fontSize: 64 }} />
            <Typography variant="h6" textAlign="center">
              Taksit #{result.installmentNumber} başarıyla ödendi
            </Typography>
            <Stack spacing={0.5} alignItems="center">
              <Typography variant="body2" color="text.secondary">
                Tutar: <strong>{formatCurrency(result.paidAmount)}</strong>
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Sağlayıcı: <strong>{result.providerName || "—"}</strong>
              </Typography>
              <Typography variant="body2" color="text.secondary">
                İşlem No: <code>{result.providerReference || "—"}</code>
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {formatDateTime(result.processedAtUtc ?? result.paymentDate)}
              </Typography>
            </Stack>
            {result.isLoanClosed ? (
              <Alert severity="success" sx={{ width: "100%" }}>
                Tebrikler! Bu krediye ait tüm taksitler ödendi ve kredi kapatıldı.
              </Alert>
            ) : null}
          </Stack>
        ) : null}

        {phase === "error" && error ? (
          <Stack spacing={2}>
            <Alert severity={error.severity} icon={<ErrorOutlineIcon />}>
              <AlertTitle>
                {error.declineCode
                  ? `Ödeme Reddedildi (${error.declineCode})`
                  : "Ödeme Tamamlanamadı"}
              </AlertTitle>
              {error.userMessage}
            </Alert>
            {error.providerName ? (
              <Typography variant="caption" color="text.secondary">
                Sağlayıcı: <strong>{error.providerName}</strong>
                {error.httpStatus ? ` · HTTP ${error.httpStatus}` : ""}
              </Typography>
            ) : null}
            {error.rawMessage && error.rawMessage !== error.userMessage ? (
              <Typography variant="caption" color="text.secondary">
                Detay: <em>{error.rawMessage}</em>
              </Typography>
            ) : null}
          </Stack>
        ) : null}
      </DialogContent>

      <DialogActions sx={{ px: 3, pb: 2 }}>
        {phase === "confirm" ? (
          <>
            <Button onClick={onCancel} color="inherit">
              Vazgeç
            </Button>
            <Button onClick={onConfirm} variant="contained">
              Ödemeyi Başlat
            </Button>
          </>
        ) : null}

        {phase === "processing" ? (
          <Button disabled>İşleminiz devam ediyor…</Button>
        ) : null}

        {phase === "success" ? (
          <Button onClick={onClose} variant="contained">
            Kapat
          </Button>
        ) : null}

        {phase === "error" ? (
          <>
            <Button onClick={onClose} color="inherit">
              Kapat
            </Button>
            {error?.retryable ? (
              <Button onClick={onRetry} variant="contained">
                Tekrar Dene
              </Button>
            ) : null}
          </>
        ) : null}
      </DialogActions>
    </Dialog>
  );
}
