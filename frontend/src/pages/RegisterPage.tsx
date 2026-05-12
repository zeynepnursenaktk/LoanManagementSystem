import { useState } from "react";
import { Link as RouterLink, useNavigate } from "react-router-dom";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CardHeader,
  Grid,
  Link,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useAuth } from "@/hooks/useAuth";
import { extractErrorMessage } from "@/api/axiosClient";
import {
  NAME_PATTERN,
  STRONG_PASSWORD_PATTERN,
  USERNAME_PATTERN,
  isValidTurkishIdentityNumber,
  isValidTurkishPhone,
} from "@/utils/validation";

const schema = z.object({
  username: z
    .string()
    .min(3, "Kullanıcı adı en az 3 karakter olmalıdır.")
    .max(64, "Kullanıcı adı 64 karakteri geçemez.")
    .regex(USERNAME_PATTERN, "Kullanıcı adı yalnızca harf, sayı, nokta, alt çizgi ve tire içerebilir."),
  password: z
    .string()
    .min(8, "Şifre en az 8 karakter olmalıdır.")
    .max(128, "Şifre 128 karakteri geçemez.")
    .regex(
      STRONG_PASSWORD_PATTERN,
      "Şifre en az bir büyük harf, bir küçük harf ve bir rakam içermelidir.",
    ),
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
    .refine(isValidTurkishIdentityNumber, "Geçerli bir T.C. Kimlik Numarası girin (11 haneli, kontrol haneleri doğru olmalı)."),
  email: z.string().email("Geçerli bir e-posta adresi girin.").max(255, "E-posta 255 karakteri geçemez."),
  phoneNumber: z
    .string()
    .optional()
    .refine(
      (v) => isValidTurkishPhone(v ?? null),
      "Geçerli bir Türkiye GSM numarası girin. Örn: 0532 123 45 67 veya +90 532 123 45 67.",
    ),
});

type RegisterForm = z.infer<typeof schema>;

export function RegisterPage(): JSX.Element {
  const { register: registerUser } = useAuth();
  const navigate = useNavigate();
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterForm>({
    resolver: zodResolver(schema),
    defaultValues: {
      username: "",
      password: "",
      firstName: "",
      lastName: "",
      identityNumber: "",
      email: "",
      phoneNumber: "",
    },
  });

  const onSubmit = async (values: RegisterForm): Promise<void> => {
    try {
      setServerError(null);
      await registerUser({
        ...values,
        phoneNumber: values.phoneNumber || null,
      });
      navigate("/dashboard", { replace: true });
    } catch (err) {
      setServerError(extractErrorMessage(err));
    }
  };

  return (
    <Box
      sx={{
        minHeight: "100vh",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        background: "linear-gradient(135deg, #1a4480 0%, #0f766e 100%)",
        py: 4,
        px: 2,
      }}
    >
      <Card sx={{ width: "100%", maxWidth: 640, borderRadius: 3 }}>
        <CardHeader
          title="Yeni hesap oluştur"
          subheader="Müşteri olarak kayıt olun"
          titleTypographyProps={{ variant: "h5", textAlign: "center", fontWeight: 700 }}
          subheaderTypographyProps={{ textAlign: "center" }}
        />
        <CardContent>
          <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
            <Stack spacing={2}>
              {serverError ? <Alert severity="error">{serverError}</Alert> : null}
              <Grid container spacing={2}>
                <Grid item xs={12} md={6}>
                  <TextField
                    label="Ad"
                    fullWidth
                    error={!!errors.firstName}
                    helperText={errors.firstName?.message}
                    {...register("firstName")}
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    label="Soyad"
                    fullWidth
                    error={!!errors.lastName}
                    helperText={errors.lastName?.message}
                    {...register("lastName")}
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    label="T.C. Kimlik Numarası"
                    fullWidth
                    inputProps={{ inputMode: "numeric", maxLength: 11 }}
                    error={!!errors.identityNumber}
                    helperText={errors.identityNumber?.message}
                    {...register("identityNumber")}
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    label="E-posta"
                    type="email"
                    fullWidth
                    error={!!errors.email}
                    helperText={errors.email?.message}
                    {...register("email")}
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    label="Telefon (opsiyonel)"
                    fullWidth
                    placeholder="+905551234567"
                    error={!!errors.phoneNumber}
                    helperText={errors.phoneNumber?.message}
                    {...register("phoneNumber")}
                  />
                </Grid>
                <Grid item xs={12} md={6}>
                  <TextField
                    label="Kullanıcı adı"
                    fullWidth
                    autoComplete="username"
                    error={!!errors.username}
                    helperText={errors.username?.message}
                    {...register("username")}
                  />
                </Grid>
                <Grid item xs={12}>
                  <TextField
                    label="Şifre"
                    type="password"
                    fullWidth
                    autoComplete="new-password"
                    error={!!errors.password}
                    helperText={errors.password?.message}
                    {...register("password")}
                  />
                </Grid>
              </Grid>
              <Button type="submit" variant="contained" size="large" disabled={isSubmitting}>
                {isSubmitting ? "Kayıt yapılıyor..." : "Kayıt Ol"}
              </Button>
              <Typography variant="body2" textAlign="center">
                Zaten hesabınız var mı?{" "}
                <Link component={RouterLink} to="/login" underline="hover">
                  Giriş yapın
                </Link>
              </Typography>
            </Stack>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
}
