using LoanManagement.Entities.Models;

namespace LoanManagement.Business.Abstract;

public interface IPaymentService
{
    Task PayInstallmentAsync(int installmentId, decimal amount);
    Task<List<Installment>> GetInstallmentsByLoanIdAsync(int loanId);
}