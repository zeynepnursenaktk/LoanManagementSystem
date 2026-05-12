using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using LoanManagement.DataAccess.Context;
using LoanManagement.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LoanManagement.Business.ExternalServices;

/// Findeks/CRIF sandbox simülasyonu.
/// Müşterinin **gerçek kredi/taksit geçmişine** (DB) bakarak dinamik skor üretir
/// ve hangi olayın skoru ne kadar etkilediğini `factors[]` olarak döner.
///
/// DelegatingHandler'lar HttpClientFactory tarafından pool'lanır (HandlerLifetime 2 dk),
/// bu nedenle scoped `LoanDbContext`'i doğrudan inject ETMEK YERİNE
/// `IServiceScopeFactory` ile her istekte yeni bir scope açar.
public sealed class CreditBureauSandboxDelegatingHandler : DelegatingHandler
{
    private static readonly Regex CustomerIdRegex = new(@"/credit-score/(?<id>\d+)$", RegexOptions.Compiled);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IServiceScopeFactory _scopeFactory;

    public CreditBureauSandboxDelegatingHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.RequestUri is null
            || !CustomerIdRegex.IsMatch(request.RequestUri.AbsolutePath))
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        await Task.Delay(120, cancellationToken).ConfigureAwait(false);

        var match = CustomerIdRegex.Match(request.RequestUri.AbsolutePath);
        if (!int.TryParse(match.Groups["id"].Value, out var customerId) || customerId <= 0)
        {
            return JsonError(HttpStatusCode.NotFound, "customer_not_found");
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LoanDbContext>();

        var customerExists = await db.Customers
            .AsNoTracking()
            .AnyAsync(c => c.Id == customerId, cancellationToken)
            .ConfigureAwait(false);

        if (!customerExists)
        {
            return JsonError(HttpStatusCode.NotFound, "customer_not_found");
        }

        var loans = await db.Loans
            .AsNoTracking()
            .Include(l => l.Installments)
            .Where(l => l.CustomerId == customerId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var nowUtc = DateTime.UtcNow;
        var breakdown = SandboxCreditScoreCalculator.Calculate(loans, nowUtc);

        var reference = $"FNDX-{nowUtc:yyyyMMdd}-{customerId:D6}";

        var payload = new
        {
            score = breakdown.Score,
            riskLevel = breakdown.RiskLevel,
            providerReference = reference,
            queriedAtUtc = nowUtc.ToString("o"),
            factors = breakdown.Factors,
        };

        var json = JsonSerializer.Serialize(payload, JsonOpts);

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        };
    }

    private static HttpResponseMessage JsonError(HttpStatusCode status, string code)
        => new(status)
        {
            Content = new StringContent(
                "{ \"error\": \"" + code + "\" }",
                System.Text.Encoding.UTF8,
                "application/json"),
        };
}
