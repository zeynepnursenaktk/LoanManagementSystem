import { createTheme } from "@mui/material/styles";
import { trTR as coreTrTR } from "@mui/material/locale";
import { trTR as gridTrTR } from "@mui/x-data-grid/locales";

export const theme = createTheme(
  {
    palette: {
      mode: "light",
      primary: { main: "#1a4480", dark: "#0f2e5a", light: "#3a6bb0" },
      secondary: { main: "#0f766e" },
      success: { main: "#15803d" },
      warning: { main: "#d97706" },
      error: { main: "#b91c1c" },
      background: {
        default: "#f5f7fa",
        paper: "#ffffff",
      },
    },
    typography: {
      fontFamily: '"Inter", "Roboto", "Helvetica", "Arial", sans-serif',
      h4: { fontWeight: 700 },
      h5: { fontWeight: 700 },
      h6: { fontWeight: 600 },
      button: { textTransform: "none", fontWeight: 600 },
    },
    shape: { borderRadius: 10 },
    components: {
      MuiPaper: { styleOverrides: { root: { backgroundImage: "none" } } },
      MuiButton: { defaultProps: { disableElevation: true } },
      MuiAppBar: { styleOverrides: { root: { backgroundColor: "#1a4480" } } },
    },
  },
  coreTrTR,
  gridTrTR,
);
