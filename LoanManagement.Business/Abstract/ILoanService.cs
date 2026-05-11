using LoanManagement.Entities.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LoanManagement.Business.Abstract;

public interface ILoanService
{
    Task<int> CreateLoanWithInstallmentsAsync(LoanRequestDto loanDto); 
    Task<LoanResponseDto?> GetLoanByIdDtoAsync(int id);
    Task<PaymentResponseDto?> PayInstallmentAsync(PaymentRequestDto request);
    Task<List<LoanResponseDto>> GetAllLoansDtoAsync();
}