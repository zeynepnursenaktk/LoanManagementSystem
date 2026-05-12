using LoanManagement.Entities.DTOs;

namespace LoanManagement.Business.Abstract;

public interface ILoanService
{
    Task<int> CreateLoanWithInstallmentsAsync(LoanRequestDto loanDto);
    Task<LoanResponseDto?> GetLoanByIdDtoAsync(int id);
    Task<List<LoanResponseDto>> GetAllLoansDtoAsync();
    Task<List<LoanResponseDto>> GetLoansByCustomerIdAsync(int customerId);
}
