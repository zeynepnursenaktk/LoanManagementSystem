using LoanManagement.Entities.DTOs;

namespace LoanManagement.Business.Abstract;

public interface ILoanService
{
    Task<int> CreateLoanAsync(LoanRequestDto loanDto);

    Task<LoanResponseDto?> GetLoanByIdAsync(int loanId);

    Task<List<LoanResponseDto>> GetLoansAsync();

    Task<List<LoanResponseDto>> GetLoansByCustomerIdAsync(int customerId);
}
