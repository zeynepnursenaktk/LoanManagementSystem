import { useEffect } from "react";
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  Stack,
  TextField,
} from "@mui/material";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "react-toastify";
import { customerService } from "@/services";
import { extractErrorMessage } from "@/api/axiosClient";
import type { CustomerListDto } from "@/types";
import {
  NAME_PATTERN,
  isValidTurkishIdentityNumber,
  isValidTurkishPhone,
} from "@/utils/validation";

const phoneMessage =
  "Geçerli bir Türkiye GSM numarası girin. Örn: 0532 123 45 67 veya +90 532 123 45 67.";

const createSchema = z.object({
  firstName: z
    .string()
    .min(2, "Ad en az 2 karakter olmalıdır.")
    .max(100, "Ad 100 karakteri geçemez.")
    .regex(NAME_PATTERN, "Ad yalnızca harf, boşluk, kesme işareti ve tire içerebilir."),
  lastName: z
    .string()
    .min(2, "Soyad en az 2 karakter olmalıdır.")
    .max(100, "Soyad 100 karakteri geçemez.")
    .regex(NAME_PATTERN, "Soyad yalnızca harf, boşluk, kesme işareti ve tire içerebilir."),
  identityNumber: z
    .string()
    .refine(
      isValidTurkishIdentityNumber,
      "Geçerli bir T.C. Kimlik Numarası girin (11 haneli, kontrol haneleri doğru olmalı).",
    ),
  email: z.string().email("Geçerli bir e-posta adresi girin.").max(255, "E-posta 255 karakteri geçemez."),
  phoneNumber: z
    .string()
    .optional()
    .refine((v) => isValidTurkishPhone(v ?? null), phoneMessage),
});

const updateSchema = z.object({
  email: z.string().email("Geçerli bir e-posta adresi girin.").max(255, "E-posta 255 karakteri geçemez."),
  phoneNumber: z
    .string()
    .optional()
    .refine((v) => isValidTurkishPhone(v ?? null), phoneMessage),
});

type CreateForm = z.infer<typeof createSchema>;
type UpdateForm = z.infer<typeof updateSchema>;

interface CustomerFormDialogProps {
  open: boolean;
  target: CustomerListDto | null;
  onClose: () => void;
  onSuccess: () => Promise<void> | void;
}

export function CustomerFormDialog({ open, target, onClose, onSuccess }: CustomerFormDialogProps): JSX.Element {
  const isEdit = !!target;

  const createForm = useForm<CreateForm>({
    resolver: zodResolver(createSchema),
    defaultValues: {
      firstName: "",
      lastName: "",
      identityNumber: "",
      email: "",
      phoneNumber: "",
    },
  });

  const updateForm = useForm<UpdateForm>({
    resolver: zodResolver(updateSchema),
    defaultValues: { email: "", phoneNumber: "" },
  });

  useEffect(() => {
    if (!open) return;
    if (isEdit && target) {
      updateForm.reset({
        email: target.email,
        phoneNumber: target.phoneNumber ?? "",
      });
    } else {
      createForm.reset({
        firstName: "",
        lastName: "",
        identityNumber: "",
        email: "",
        phoneNumber: "",
      });
    }
  }, [open, isEdit, target, createForm, updateForm]);

  const submitCreate = async (values: CreateForm): Promise<void> => {
    try {
      const res = await customerService.create({
        ...values,
        phoneNumber: values.phoneNumber || null,
      });
      toast.success(res.message ?? "Müşteri oluşturuldu.");
      await onSuccess();
    } catch (err) {
      toast.error(extractErrorMessage(err));
    }
  };

  const submitUpdate = async (values: UpdateForm): Promise<void> => {
    if (!target) return;
    try {
      const res = await customerService.update(target.id, {
        email: values.email,
        phoneNumber: values.phoneNumber || null,
      });
      toast.success(res.message ?? "Müşteri güncellendi.");
      await onSuccess();
    } catch (err) {
      toast.error(extractErrorMessage(err));
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{isEdit ? "Müşteri güncelle" : "Yeni müşteri ekle"}</DialogTitle>
      <DialogContent>
        {isEdit ? (
          <Stack
            component="form"
            id="customer-form"
            spacing={2}
            sx={{ mt: 1 }}
            onSubmit={updateForm.handleSubmit(submitUpdate)}
          >
            <Alert severity="info">
              Yalnızca e-posta ve telefon güncellenebilir. Ad, soyad ve TC değişikliği için yeni kayıt gerekir.
            </Alert>
            <TextField
              label="E-posta"
              type="email"
              fullWidth
              error={!!updateForm.formState.errors.email}
              helperText={updateForm.formState.errors.email?.message}
              {...updateForm.register("email")}
            />
            <TextField
              label="Telefon (opsiyonel)"
              fullWidth
              placeholder="+905551234567"
              error={!!updateForm.formState.errors.phoneNumber}
              helperText={updateForm.formState.errors.phoneNumber?.message}
              {...updateForm.register("phoneNumber")}
            />
          </Stack>
        ) : (
          <Grid
            container
            spacing={2}
            component="form"
            id="customer-form"
            onSubmit={createForm.handleSubmit(submitCreate)}
            sx={{ mt: 1 }}
          >
            <Grid item xs={12} md={6}>
              <TextField
                label="Ad"
                fullWidth
                error={!!createForm.formState.errors.firstName}
                helperText={createForm.formState.errors.firstName?.message}
                {...createForm.register("firstName")}
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField
                label="Soyad"
                fullWidth
                error={!!createForm.formState.errors.lastName}
                helperText={createForm.formState.errors.lastName?.message}
                {...createForm.register("lastName")}
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                label="T.C. Kimlik Numarası"
                fullWidth
                inputProps={{ inputMode: "numeric", maxLength: 11 }}
                error={!!createForm.formState.errors.identityNumber}
                helperText={createForm.formState.errors.identityNumber?.message}
                {...createForm.register("identityNumber")}
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField
                label="E-posta"
                type="email"
                fullWidth
                error={!!createForm.formState.errors.email}
                helperText={createForm.formState.errors.email?.message}
                {...createForm.register("email")}
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <TextField
                label="Telefon (opsiyonel)"
                fullWidth
                placeholder="+905551234567"
                error={!!createForm.formState.errors.phoneNumber}
                helperText={createForm.formState.errors.phoneNumber?.message}
                {...createForm.register("phoneNumber")}
              />
            </Grid>
          </Grid>
        )}
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={onClose} color="inherit">
          Vazgeç
        </Button>
        <Button
          type="submit"
          form="customer-form"
          variant="contained"
          disabled={isEdit ? updateForm.formState.isSubmitting : createForm.formState.isSubmitting}
        >
          {isEdit ? "Güncelle" : "Oluştur"}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
