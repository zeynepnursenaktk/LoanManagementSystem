using LoanManagement.Entities.Models;
using LoanManagement.Entities.DTOs;
namespace LoanManagement.Business.Abstract;

public interface ILoanService
{
    Task CreateLoanWithInstallmentsAsync(Loan loan);
    Task<LoanResponseDto?> GetLoanByIdDtoAsync(int id);
    Task<PaymentResponseDto?> PayInstallmentAsync(PaymentRequestDto request);
    Task<List<LoanResponseDto>> GetAllLoansDtoAsync();
}