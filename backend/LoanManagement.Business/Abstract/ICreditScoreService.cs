namespace LoanManagement.Business.Abstract;

public interface ICreditScoreService
{
    Task<int> GetCreditScoreAsync(int customerId);
}