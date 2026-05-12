import { Navigate, Route, Routes } from "react-router-dom";
import { ToastContainer } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";
import { AppLayout } from "@/components/AppLayout";
import { ProtectedRoute } from "@/components/ProtectedRoute";
import { LoginPage } from "@/pages/LoginPage";
import { RegisterPage } from "@/pages/RegisterPage";
import { DashboardPage } from "@/pages/DashboardPage";
import { CustomersListPage } from "@/pages/customers/CustomersListPage";
import { CustomerDetailPage } from "@/pages/customers/CustomerDetailPage";
import { LoansListPage } from "@/pages/loans/LoansListPage";
import { LoanDetailPage } from "@/pages/loans/LoanDetailPage";
import { PaymentsListPage } from "@/pages/payments/PaymentsListPage";
import { OverdueInstallmentsPage } from "@/pages/installments/OverdueInstallmentsPage";
import { NotFoundPage } from "@/pages/NotFoundPage";

export function App(): JSX.Element {
  return (
    <>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />

        <Route
          element={
            <ProtectedRoute>
              <AppLayout />
            </ProtectedRoute>
          }
        >
          <Route index element={<Navigate to="/dashboard" replace />} />
          <Route path="/dashboard" element={<DashboardPage />} />

          <Route
            path="/customers"
            element={
              <ProtectedRoute roles={["Admin"]}>
                <CustomersListPage />
              </ProtectedRoute>
            }
          />
          <Route path="/customers/:id" element={<CustomerDetailPage />} />

          <Route path="/loans" element={<LoansListPage />} />
          <Route path="/loans/:id" element={<LoanDetailPage />} />

          <Route path="/payments" element={<PaymentsListPage />} />
          <Route path="/installments/overdue" element={<OverdueInstallmentsPage />} />
        </Route>

        <Route path="*" element={<NotFoundPage />} />
      </Routes>

      <ToastContainer
        position="top-right"
        autoClose={4000}
        hideProgressBar={false}
        newestOnTop
        closeOnClick
        pauseOnHover
        theme="light"
      />
    </>
  );
}
