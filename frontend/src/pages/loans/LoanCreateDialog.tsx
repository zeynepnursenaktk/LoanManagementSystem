import { useEffect, useState } from "react";
import {
  Alert,
  Autocomplete,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  MenuItem,
  Stack,
  TextField,
} from "@mui/material";
import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "react-toastify";
import { customerService, loanService } from "@/services";
import { extractErrorMessage } from "@/api/axiosClient";
import { useAuth } from "@/hooks/useAuth";
import { LoanType, LoanTypeLabels } from "@/types";
import type { CustomerListDto } from "@/types";

const schema = z
  .object({
    customerId: z.number().int().positive("Müşteri seçimi zorunludur."),
    amount: z
      .number({ invalid_type_error: "Tutar zorunludur." })
      .positive("Tutar 0'dan büyük olmalıdır.")
      .max(1_000_000_000, "Tutar en fazla 1.000.000.000 olabilir."),
    tenor: z
      .number({ invalid_type_error: "Vade zorunludur." })
      .int()
      .min(1, "Vade en az 1 ay olmalıdır.")
      .max(120, "Vade en fazla 120 ay olabilir."),
    profitRate: z
      .number({ invalid_type_error: "Kar oranı zorunludur." })
      .min(0, "Kar oranı 0'dan küçük olamaz.")
      .max(100, "Kar oranı 100'ü geçemez."),
    startDate: z.string().min(1, "Başlangıç tarihi zorunludur."),
    loanType: z.nativeEnum(LoanType),
  });

type LoanForm = z.infer<typeof schema>;

interface LoanCreateDialogProps {
  open: boolean;
  onClose: () => void;
  onSuccess: () => Promise<void> | void;
}

export function LoanCreateDialog({ open, onClose, onSuccess }: LoanCreateDialogProps): JSX.Element {
  const { user, isAdmin } = useAuth();
  const [customers, setCustomers] = useState<CustomerListDto[]>([]);
  const [loadingCustomers, setLoadingCustomers] = useState(false);

  const today = new Date().toISOString().slice(0, 10);

  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<LoanForm>({
    resolver: zodResolver(schema),
    defaultValues: {
      customerId: user?.customerId ?? 0,
      amount: undefined as unknown as number,
      tenor: 12,
      profitRate: 24,
      startDate: today,
      loanType: LoanType.Personal,
    },
  });

  useEffect(() => {
    if (!open) return;
    reset({
      customerId: user?.customerId ?? 0,
      amount: undefined as unknown as number,
      tenor: 12,
      profitRate: 24,
      startDate: today,
      loanType: LoanType.Personal,
    });
    if (isAdmin) {
      setLoadingCustomers(true);
      customerService
        .list()
        .then(setCustomers)
        .catch((err) => toast.error(extractErrorMessage(err)))
        .finally(() => setLoadingCustomers(false));
    }
  }, [open, isAdmin, reset, user?.customerId, today]);

  const onSubmit = async (values: LoanForm): Promise<void> => {
    try {
      await loanService.create({
        customerId: isAdmin ? values.customerId : user?.customerId ?? values.customerId,
        amount: values.amount,
        tenor: values.tenor,
        profitRate: values.profitRate,
        startDate: new Date(values.startDate).toISOString(),
        loanType: values.loanType,
      });
      toast.success("Kredi başarıyla oluşturuldu.");
      await onSuccess();
    } catch (err) {
      toast.error(extractErrorMessage(err));
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>Yeni Kredi Başvurusu</DialogTitle>
      <DialogContent>
        <Stack
          component="form"
          id="loan-create-form"
          spacing={2}
          sx={{ mt: 1 }}
          onSubmit={handleSubmit(onSubmit)}
        >
          <Alert severity="info">
            Yıllık kar oranı yüzde olarak girilir. Örn. <strong>24</strong> = %24 yıllık.
          </Alert>

          {isAdmin ? (
            <Controller
              control={control}
              name="customerId"
              render={({ field }) => (
                <Autocomplete
                  loading={loadingCustomers}
                  options={customers}
                  getOptionLabel={(c) => `${c.firstName} ${c.lastName} (#${c.id})`}
                  isOptionEqualToValue={(o, v) => o.id === v.id}
                  value={customers.find((c) => c.id === field.value) ?? null}
                  onChange={(_, val) => field.onChange(val?.id ?? 0)}
                  renderInput={(params) => (
                    <TextField
                      {...params}
                      label="Müşteri"
                      error={!!errors.customerId}
                      helperText={errors.customerId?.message}
                    />
                  )}
                />
              )}
            />
          ) : null}

          <Grid container spacing={2}>
            <Grid item xs={12} sm={6}>
              <TextField
                label="Kredi Türü"
                select
                fullWidth
                defaultValue={LoanType.Personal}
                error={!!errors.loanType}
                helperText={errors.loanType?.message}
                {...register("loanType", { valueAsNumber: true })}
              >
                {Object.values(LoanType)
                  .filter((v): v is LoanType => typeof v === "number")
                  .map((t) => (
                    <MenuItem key={t} value={t}>
                      {LoanTypeLabels[t]}
                    </MenuItem>
                  ))}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                label="Başlangıç Tarihi"
                type="date"
                fullWidth
                InputLabelProps={{ shrink: true }}
                error={!!errors.startDate}
                helperText={errors.startDate?.message}
                {...register("startDate")}
              />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField
                label="Tutar (₺)"
                type="number"
                fullWidth
                inputProps={{ min: 0, step: "0.01" }}
                error={!!errors.amount}
                helperText={errors.amount?.message}
                {...register("amount", { valueAsNumber: true })}
              />
            </Grid>
            <Grid item xs={6} sm={3}>
              <TextField
                label="Vade (ay)"
                type="number"
                fullWidth
                inputProps={{ min: 1, max: 120, step: 1 }}
                error={!!errors.tenor}
                helperText={errors.tenor?.message}
                {...register("tenor", { valueAsNumber: true })}
              />
            </Grid>
            <Grid item xs={6} sm={3}>
              <TextField
                label="Yıllık Kar (%)"
                type="number"
                fullWidth
                inputProps={{ min: 0, max: 100, step: "0.01" }}
                error={!!errors.profitRate}
                helperText={errors.profitRate?.message}
                {...register("profitRate", { valueAsNumber: true })}
              />
            </Grid>
          </Grid>
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={onClose} color="inherit">
          Vazgeç
        </Button>
        <Button type="submit" form="loan-create-form" variant="contained" disabled={isSubmitting}>
          Başvuruyu Oluştur
        </Button>
      </DialogActions>
    </Dialog>
  );
}
