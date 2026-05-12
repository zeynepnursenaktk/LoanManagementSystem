using LoanManagement.Business.Abstract;
using LoanManagement.Business.ExternalServices;
using LoanManagement.DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LoanManagement.Business.Services;

/// `ICreditScoreService`'i dış kredi bürosu (Findeks Mock) üzerinden çözer.
/// Mevcut çağrı yüzeyini bozmadan skor sağlayıcısını dışsallaştırır.
public sealed class ExternalCreditScoreService : ICreditScoreService, IExternalCreditScoreService
{
    private readonly LoanDbContext _context;
    private readonly IExternalCreditBureauClient _bureau;
    private readonly ILogger<ExternalCreditScoreService> _logger;

    public ExternalCreditScoreService(
        LoanDbContext context,
        IExternalCreditBureauClient bureau,
        ILogger<ExternalCreditScoreService> logger)
    {
        _context = context;
        _bureau = bureau;
        _logger = logger;
    }

    public async Task<int> GetCreditScoreAsync(int customerId)
    {
        var result = await GetDetailedScoreAsync(customerId).ConfigureAwait(false);
        return result.Score;
    }

    public async Task<CreditBureauScoreResult> GetDetailedScoreAsync(int customerId)
    {
        var customerExists = await _context.Customers
            .AnyAsync(c => c.Id == customerId && !c.IsDeleted)
            .ConfigureAwait(false);

        if (!customerExists)
            throw new KeyNotFoundException($"Customer with id {customerId} not found.");

        try
        {
            var result = await _bureau.GetScoreAsync(customerId).ConfigureAwait(false);
            _logger.LogInformation(
                "Credit score fetched for customer {CustomerId} = {Score} ({Risk}) from {Provider}",
                customerId, result.Score, result.RiskLevel, result.ProviderName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch credit score from external bureau for {CustomerId}", customerId);
            throw new InvalidOperationException(
                "Kredi skoru sağlayıcısına ulaşılamadı. Lütfen daha sonra tekrar deneyin.", ex);
        }
    }
}
