using LoanManagement.Entities.Models;
using LoanManagement.Entities.DTOs; 
namespace LoanManagement.Business.Abstract;

public interface ILoanService
{
    // Bir kredi kaydı oluştururken aynı zamanda taksitlerini de otomatik hesaplayıp kaydedecek metot
    Task CreateLoanWithInstallmentsAsync(Loan loan);
    
    // Müşterinin veya bankanın kredileri listelemesi için
    Task<List<Loan>> GetAllLoansAsync();

    Task<LoanResponseDto?> GetLoanByIdDtoAsync(int id);
}