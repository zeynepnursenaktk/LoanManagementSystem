namespace LoanManagement.Business.ExternalServices;

/// Dış kredi bürosu (CRM bağımsız) sorgulama soyutlaması.
public interface IExternalCreditBureauClient
{
    Task<CreditBureauScoreResult> GetScoreAsync(
        int customerId,
        CancellationToken cancellationToken = default);
}
