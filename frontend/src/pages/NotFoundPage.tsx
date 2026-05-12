import { Box, Button, Stack, Typography } from "@mui/material";
import { useNavigate } from "react-router-dom";

export function NotFoundPage(): JSX.Element {
  const navigate = useNavigate();
  return (
    <Box sx={{ minHeight: "60vh", display: "flex", alignItems: "center", justifyContent: "center" }}>
      <Stack spacing={2} alignItems="center">
        <Typography variant="h2" sx={{ fontWeight: 800 }}>
          404
        </Typography>
        <Typography variant="h6">Sayfa bulunamadı.</Typography>
        <Button variant="contained" onClick={() => navigate("/dashboard")}>
          Panele Dön
        </Button>
      </Stack>
    </Box>
  );
}
