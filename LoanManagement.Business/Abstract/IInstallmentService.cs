using LoanManagement.Entities.DTOs;

namespace LoanManagement.Business.Abstract;

public interface IInstallmentService
{
    Task<List<InstallmentDto>> GetByLoanIdAsync(int loanId);
    Task<InstallmentDto?> GetByIdAsync(int id);
    Task<List<InstallmentDto>> GetUnpaidByCustomerIdAsync(int customerId);
    Task<List<InstallmentDto>> GetOverdueByCustomerIdAsync(int customerId);

    /// Vadesi geçmiş ödenmemiş taksitlerin durumunu Overdue olarak günceller.
    Task<int> UpdateOverdueInstallmentsAsync();
}
