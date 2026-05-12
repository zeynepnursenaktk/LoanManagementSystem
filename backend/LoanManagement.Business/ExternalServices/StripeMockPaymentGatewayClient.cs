using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LoanManagement.Business.ExternalServices;

/// Stripe Sandbox `POST /v1/payment_intents` sözleşmesine uyan typed HttpClient.
/// Yanıtı `PaymentGatewayChargeResult`'a normalize eder.
public sealed class StripeMockPaymentGatewayClient : IExternalPaymentGatewayClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StripeMockPaymentGatewayClient> _logger;
    private readonly PaymentGatewayOptions _options;

    public StripeMockPaymentGatewayClient(
        HttpClient httpClient,
        IOptions<ExternalServicesOptions> options,
        ILogger<StripeMockPaymentGatewayClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value.PaymentGateway;
        _logger = logger;
    }

    public async Task<PaymentGatewayChargeResult> ChargeAsync(
        PaymentGatewayChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        // Stripe API en yakın küçük birime kuruş (long) bekler.
        long amountMinor = (long)Math.Round(request.Amount * 100m, MidpointRounding.AwayFromZero);

        var payload = new
        {
            amount = amountMinor,
            currency = request.Currency.ToLowerInvariant(),
            description = request.Description,
            metadata = request.Metadata,
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "payment_intents")
        {
            Content = JsonContent.Create(payload),
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            httpRequest.Headers.Add("Idempotency-Key", request.IdempotencyKey);

        try
        {
            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            var body = await response.Content
                .ReadFromJsonAsync<StripePaymentIntentResponse>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (body is null)
            {
                _logger.LogWarning("Stripe sandbox empty body — status {Status}", response.StatusCode);
                return Failure(request, PaymentDeclineCodes.ProcessingError, "Ödeme sağlayıcısı boş yanıt döndürdü.");
            }

            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
            {
                if (string.Equals(body.Status, "succeeded", StringComparison.OrdinalIgnoreCase))
                {
                    return new PaymentGatewayChargeResult
                    {
                        Status = PaymentGatewayStatus.Succeeded,
                        ProviderName = _options.ProviderName,
                        ProviderReference = body.Id ?? string.Empty,
                        Message = "Ödeme başarıyla gerçekleştirildi.",
                        Amount = request.Amount,
                        Currency = request.Currency,
                        ProcessedAtUtc = DateTime.UtcNow,
                    };
                }

                return Failure(request, body.LastPaymentError?.DeclineCode ?? body.LastPaymentError?.Code ?? PaymentDeclineCodes.ProcessingError,
                    body.LastPaymentError?.Message ?? "Ödeme tamamlanamadı.");
            }

            // 402 (Payment Required) — Stripe'in en yaygın decline yanıt kodu
            if (response.StatusCode == HttpStatusCode.PaymentRequired || (int)response.StatusCode == 402)
            {
                return Failure(request,
                    body.Error?.DeclineCode ?? body.Error?.Code ?? PaymentDeclineCodes.CardDeclined,
                    body.Error?.Message ?? "Ödeme reddedildi.");
            }

            _logger.LogWarning("Stripe sandbox unexpected status {Status} for amount {Amount}", response.StatusCode, request.Amount);
            return Failure(request, PaymentDeclineCodes.ProcessingError,
                body.Error?.Message ?? $"Ödeme sağlayıcısı beklenmeyen yanıt verdi (HTTP {(int)response.StatusCode}).");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Stripe sandbox timeout for {Amount}", request.Amount);
            return Timeout(request);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Stripe sandbox network error for {Amount}", request.Amount);
            return new PaymentGatewayChargeResult
            {
                Status = PaymentGatewayStatus.Failed,
                ProviderName = _options.ProviderName,
                ProviderReference = string.Empty,
                DeclineCode = PaymentDeclineCodes.GatewayTimeout,
                Message = "Ödeme sağlayıcısına ulaşılamadı, lütfen daha sonra tekrar deneyin.",
                Amount = request.Amount,
                Currency = request.Currency,
            };
        }
    }

    private PaymentGatewayChargeResult Failure(PaymentGatewayChargeRequest request, string declineCode, string message)
        => new()
        {
            Status = PaymentGatewayStatus.Declined,
            ProviderName = _options.ProviderName,
            ProviderReference = string.Empty,
            DeclineCode = declineCode,
            Message = message,
            Amount = request.Amount,
            Currency = request.Currency,
        };

    private PaymentGatewayChargeResult Timeout(PaymentGatewayChargeRequest request)
        => new()
        {
            Status = PaymentGatewayStatus.Failed,
            ProviderName = _options.ProviderName,
            ProviderReference = string.Empty,
            DeclineCode = PaymentDeclineCodes.GatewayTimeout,
            Message = "Ödeme sağlayıcısı yanıt zaman aşımına uğradı.",
            Amount = request.Amount,
            Currency = request.Currency,
        };

    private sealed class StripePaymentIntentResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("last_payment_error")]
        public StripeError? LastPaymentError { get; set; }

        [JsonPropertyName("error")]
        public StripeError? Error { get; set; }
    }

    private sealed class StripeError
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("decline_code")]
        public string? DeclineCode { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    // tek noktada CultureInfo.InvariantCulture kullanımını koruyalım.
    private static readonly CultureInfo _invariant = CultureInfo.InvariantCulture;
    static StripeMockPaymentGatewayClient() => _ = _invariant;
}
