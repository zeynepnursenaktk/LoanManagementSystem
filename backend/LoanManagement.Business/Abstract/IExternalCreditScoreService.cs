using LoanManagement.Business.ExternalServices;

namespace LoanManagement.Business.Abstract;

/// `ICreditScoreService` ek olarak provider metadata'sını (riskLevel, queriedAt, ref) sunar.
/// Controller bunu kullanarak frontend'e zengin yanıt döner.
public interface IExternalCreditScoreService
{
    Task<CreditBureauScoreResult> GetDetailedScoreAsync(int customerId);
}
