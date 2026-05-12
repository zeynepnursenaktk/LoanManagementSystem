using System.Net;
using System.Text.Json;

namespace LoanManagement.Business.ExternalServices;

/// Stripe sandbox endpoint'lerine "gerçek" istek atılmasını engelleyen, deterministic
/// senaryolar üreten DelegatingHandler. Dış ağ çağrısı yerine doğrudan response döner.
/// — UseSandboxHandler=true iken devreye girer.
///
/// SENARYOLAR (amount'a bağlı deterministic, banking sektörü sandbox standardı):
/// - amount &lt;= 0                : 400 invalid_amount
/// - amount.cents % 100 == 1      : 402 insufficient_funds
/// - amount.cents % 100 == 2      : 402 card_declined
/// - amount.cents % 100 == 3      : 402 expired_card
/// - amount.cents % 100 == 4      : 504 gateway_timeout (HttpRequestException simülasyonu)
/// - amount.cents % 100 == 5      : 500 processing_error
/// - amount &gt; 100_000_000 cent : 402 fraudulent
/// - diğer hepsi                  : 200 succeeded
public sealed class StripeSandboxDelegatingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Yalnızca payment_intents path'i için simülasyon (diğer endpoint'ler bypass eder).
        if (request.RequestUri is null
            || !request.RequestUri.AbsolutePath.EndsWith("payment_intents", StringComparison.OrdinalIgnoreCase))
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        // Gerçek ağ latency'sini simüle et — bankacılık testlerinde realistic olması için.
        await Task.Delay(180, cancellationToken).ConfigureAwait(false);

        // Body'den amount oku.
        long amountMinor = 0;
        string currency = "try";
        if (request.Content is not null)
        {
            var raw = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                if (root.TryGetProperty("amount", out var amt) && amt.TryGetInt64(out var n))
                    amountMinor = n;
                if (root.TryGetProperty("currency", out var cur))
                    currency = cur.GetString() ?? "try";
            }
            catch
            {
                /* corrupt body — varsayılan ile devam */
            }
        }

        var idempotencyKey = request.Headers.TryGetValues("Idempotency-Key", out var values)
            ? values.FirstOrDefault() ?? Guid.NewGuid().ToString("N")
            : Guid.NewGuid().ToString("N");

        var (status, payload) = BuildScenario(amountMinor, currency, idempotencyKey);

        return new HttpResponseMessage(status)
        {
            Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json"),
        };
    }

    private static (HttpStatusCode Status, string Body) BuildScenario(long amountMinor, string currency, string idempotencyKey)
    {
        if (amountMinor <= 0)
        {
            return (HttpStatusCode.BadRequest, BuildError("invalid_amount", "invalid_amount", "Geçersiz tutar."));
        }

        if (amountMinor > 100_000_000) // > 1.000.000 TRY
        {
            return ((HttpStatusCode)402, BuildError("card_declined", "fraudulent", "İşlem güvenlik nedeniyle reddedildi."));
        }

        long lastTwo = amountMinor % 100;
        switch (lastTwo)
        {
            case 1:
                return ((HttpStatusCode)402, BuildError("card_declined", "insufficient_funds", "Kart bakiyesi/limiti yetersiz."));
            case 2:
                return ((HttpStatusCode)402, BuildError("card_declined", "card_declined", "Kart reddedildi."));
            case 3:
                return ((HttpStatusCode)402, BuildError("expired_card", "expired_card", "Kartın son kullanma tarihi geçmiş."));
            case 4:
                return (HttpStatusCode.GatewayTimeout, BuildError("processing_error", "gateway_timeout", "Ödeme sağlayıcısı yanıt vermiyor."));
            case 5:
                return (HttpStatusCode.InternalServerError, BuildError("processing_error", "processing_error", "İşlem sırasında beklenmeyen hata."));
        }

        // Başarı senaryosu — Stripe PaymentIntent yanıt şekli.
        var ok = $$"""
            {
              "id": "pi_sandbox_{{idempotencyKey[..Math.Min(12, idempotencyKey.Length)]}}",
              "object": "payment_intent",
              "status": "succeeded",
              "amount": {{amountMinor}},
              "currency": "{{currency}}"
            }
            """;
        return (HttpStatusCode.OK, ok);
    }

    private static string BuildError(string code, string declineCode, string message)
        => $$"""
            {
              "error": {
                "code": "{{code}}",
                "decline_code": "{{declineCode}}",
                "message": "{{EscapeJson(message)}}"
              }
            }
            """;

    private static string EscapeJson(string s) => s.Replace("\"", "\\\"");
}
