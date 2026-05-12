import { Chip } from "@mui/material";
import type { InstallmentStatus, LoanStatus } from "@/types";
import { InstallmentStatusLabels, LoanStatusLabels } from "@/types";

type ChipColor = "default" | "primary" | "secondary" | "error" | "info" | "success" | "warning";

const loanColors: Record<LoanStatus, ChipColor> = {
  Active: "primary",
  Closed: "default",
};

const installmentColors: Record<InstallmentStatus, ChipColor> = {
  Unpaid: "warning",
  Paid: "success",
  Overdue: "error",
};

interface LoanStatusChipProps {
  status: LoanStatus;
}

export function LoanStatusChip({ status }: LoanStatusChipProps): JSX.Element {
  return <Chip size="small" label={LoanStatusLabels[status] ?? status} color={loanColors[status] ?? "default"} />;
}

interface InstallmentStatusChipProps {
  status: InstallmentStatus;
}

export function InstallmentStatusChip({ status }: InstallmentStatusChipProps): JSX.Element {
  return (
    <Chip
      size="small"
      label={InstallmentStatusLabels[status] ?? status}
      color={installmentColors[status] ?? "default"}
    />
  );
}
