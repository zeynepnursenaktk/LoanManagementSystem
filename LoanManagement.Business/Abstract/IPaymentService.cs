using LoanManagement.Entities.Models;

namespace LoanManagement.Business.Abstract;

public interface IPaymentService
{
    Task<List<Payment>> GetAllPaymentsAsync();

    Task<List<Installment>> GetInstallmentsByLoanIdAsync(int loanId);
}