import { useState } from "react";
import { Link as RouterLink, useLocation, useNavigate } from "react-router-dom";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CardHeader,
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

const schema = z.object({
  username: z.string().min(3, "Kullanıcı adı zorunludur."),
  password: z.string().min(4, "Şifre en az 4 karakter olmalıdır."),
});

type LoginForm = z.infer<typeof schema>;

export function LoginPage(): JSX.Element {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({
    resolver: zodResolver(schema),
    defaultValues: { username: "", password: "" },
  });

  const onSubmit = async (values: LoginForm): Promise<void> => {
    try {
      setServerError(null);
      await login(values);
      const target = (location.state as { from?: { pathname?: string } } | null)?.from?.pathname ?? "/dashboard";
      navigate(target, { replace: true });
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
        px: 2,
      }}
    >
      <Card sx={{ width: "100%", maxWidth: 420, borderRadius: 3 }}>
        <CardHeader
          title="Kredi Takip Sistemi"
          subheader="Hesabınıza giriş yapın"
          titleTypographyProps={{ variant: "h5", textAlign: "center", fontWeight: 700 }}
          subheaderTypographyProps={{ textAlign: "center" }}
        />
        <CardContent>
          <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
            <Stack spacing={2}>
              {serverError ? <Alert severity="error">{serverError}</Alert> : null}
              <TextField
                label="Kullanıcı adı"
                fullWidth
                autoComplete="username"
                autoFocus
                error={!!errors.username}
                helperText={errors.username?.message}
                {...register("username")}
              />
              <TextField
                label="Şifre"
                type="password"
                fullWidth
                autoComplete="current-password"
                error={!!errors.password}
                helperText={errors.password?.message}
                {...register("password")}
              />
              <Button type="submit" variant="contained" size="large" disabled={isSubmitting}>
                {isSubmitting ? "Giriş yapılıyor..." : "Giriş Yap"}
              </Button>
              <Typography variant="body2" textAlign="center">
                Hesabınız yok mu?{" "}
                <Link component={RouterLink} to="/register" underline="hover">
                  Kayıt olun
                </Link>
              </Typography>
              <Alert severity="info">
                <strong>Test admin:</strong> admin / Admin123!
              </Alert>
            </Stack>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
}
