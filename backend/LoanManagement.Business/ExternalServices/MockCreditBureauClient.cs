using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LoanManagement.Business.ExternalServices;

/// CRIF / Findeks benzeri kredi bürosu sandbox client'ı.
/// Sözleşme: `GET credit-score/{customerId}` → `{ score, riskLevel, providerReference, queriedAt }`
public sealed class MockCreditBureauClient : IExternalCreditBureauClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MockCreditBureauClient> _logger;
    private readonly CreditBureauOptions _options;

    public MockCreditBureauClient(
        HttpClient httpClient,
        IOptions<ExternalServicesOptions> options,
        ILogger<MockCreditBureauClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value.CreditBureau;
        _logger = logger;
    }

    public async Task<CreditBureauScoreResult> GetScoreAsync(int customerId, CancellationToken cancellationToken = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"credit-score/{customerId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        try
        {
            using var response = await _httpClient.SendAsync(req, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var body = await response.Content
                .ReadFromJsonAsync<BureauScoreResponse>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (body is null)
                throw new InvalidOperationException("Kredi bürosu yanıtı boş geldi.");

            return new CreditBureauScoreResult
            {
                CustomerId = customerId,
                Score = body.Score,
                RiskLevel = body.RiskLevel ?? ClassifyRisk(body.Score),
                ProviderName = _options.ProviderName,
                ProviderReference = body.ProviderReference ?? string.Empty,
                QueriedAtUtc = body.QueriedAtUtc ?? DateTime.UtcNow,
                IsFresh = true,
                Factors = body.Factors is { Count: > 0 }
                    ? body.Factors
                        .Where(f => !string.IsNullOrEmpty(f.Code))
                        .Select(f => new CreditScoreFactor(f.Code!, f.Label ?? f.Code!, f.Delta))
                        .ToList()
                    : Array.Empty<CreditScoreFactor>(),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Credit bureau call failed for customer {CustomerId}", customerId);
            throw;
        }
    }

    public static string ClassifyRisk(int score) => SandboxCreditScoreCalculator.ClassifyRisk(score);

    private sealed class BureauScoreResponse
    {
        [JsonPropertyName("score")]
        public int Score { get; set; }

        [JsonPropertyName("riskLevel")]
        public string? RiskLevel { get; set; }

        [JsonPropertyName("providerReference")]
        public string? ProviderReference { get; set; }

        [JsonPropertyName("queriedAtUtc")]
        public DateTime? QueriedAtUtc { get; set; }

        [JsonPropertyName("factors")]
        public List<BureauScoreFactor>? Factors { get; set; }
    }

    private sealed class BureauScoreFactor
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("delta")]
        public int Delta { get; set; }
    }
}
