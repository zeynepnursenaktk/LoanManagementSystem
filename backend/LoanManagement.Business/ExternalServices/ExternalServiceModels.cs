namespace LoanManagement.Business.ExternalServices;

/// Ödeme ağ geçidi işlem sonuç durumu (Stripe-uyumlu).
public enum PaymentGatewayStatus
{
    /// İşlem başarıyla tamamlandı.
    Succeeded = 1,
    /// İşlem banka/kart tarafından reddedildi (declineCode dolu olur).
    Declined = 2,
    /// Ağ veya gateway tarafında geçici hata (timeout, 5xx).
    Failed = 3,
}

/// Ödeme reddetme nedenleri (Stripe decline_code uyumlu, locale-bağımsız anahtar).
public static class PaymentDeclineCodes
{
    public const string InsufficientFunds = "insufficient_funds";
    public const string CardDeclined = "card_declined";
    public const string ExpiredCard = "expired_card";
    public const string IncorrectCvc = "incorrect_cvc";
    public const string ProcessingError = "processing_error";
    public const string GatewayTimeout = "gateway_timeout";
    public const string InvalidAmount = "invalid_amount";
    public const string Fraudulent = "fraudulent";
}

/// Ödeme isteği — dış sağlayıcıya gönderilecek alanlar.
public sealed class PaymentGatewayChargeRequest
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "TRY";
    public string Description { get; init; } = string.Empty;
    /// Tekrar denemelerde aynı tutulmalı, çift ödeme oluşmasın.
    public string IdempotencyKey { get; init; } = Guid.NewGuid().ToString("N");
    /// İlişkili kayıt (LoanId / InstallmentId) — sağlayıcıya metadata olarak iletilir.
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

/// Ödeme ağ geçidi yanıtı — başarı/başarısızlık tek bir DTO altında.
public sealed class PaymentGatewayChargeResult
{
    public PaymentGatewayStatus Status { get; init; }
    public string ProviderName { get; init; } = string.Empty;
    public string ProviderReference { get; init; } = string.Empty;
    public string? DeclineCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "TRY";
    public DateTime ProcessedAtUtc { get; init; } = DateTime.UtcNow;

    public bool IsSuccess => Status == PaymentGatewayStatus.Succeeded;
}

/// Kredi büro sorgu sonucu.
public sealed class CreditBureauScoreResult
{
    public int CustomerId { get; init; }
    public int Score { get; init; }
    public string RiskLevel { get; init; } = "Unknown";
    public string ProviderName { get; init; } = string.Empty;
    public string ProviderReference { get; init; } = string.Empty;
    public DateTime QueriedAtUtc { get; init; } = DateTime.UtcNow;
    public bool IsFresh { get; init; } = true;

    /// "Bu skor neden bu?" sorusunun yanıtı — faktör başına +/- katkı.
    /// Production sağlayıcısı (Findeks/CRIF) sağlamayabilir; o zaman boş liste döner.
    public IReadOnlyList<CreditScoreFactor> Factors { get; init; } = Array.Empty<CreditScoreFactor>();
}

/// Kredi skorunu oluşturan tek bir bileşen.
/// Örn: { code: "closed_loan_bonus", label: "2 kapatılmış kredi", delta: 300 }
public sealed record CreditScoreFactor(string Code, string Label, int Delta);
