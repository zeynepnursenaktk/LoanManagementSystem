namespace LoanManagement.Entities.DTOs;

public class PaymentResponseDto
{
    public string Message { get; set; } = null!;

    /// "Succeeded" | "Declined" | "Failed". Frontend hata akışı bu alanı kullanır.
    public string Status { get; set; } = "Succeeded";

    /// İşlem reddedildiyse Stripe-uyumlu decline kodu (insufficient_funds, card_declined, expired_card, ...). Başarılıda null.
    public string? DeclineCode { get; set; }

    /// İşleyen dış sağlayıcı adı (örn. "Stripe Sandbox").
    public string ProviderName { get; set; } = string.Empty;

    /// Dış sağlayıcı işlem referansı (Stripe PaymentIntent id'si gibi). Reddedildiyse boş olabilir.
    public string ProviderReference { get; set; } = string.Empty;

    public int PaymentId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = null!;
    public int LoanId { get; set; }
    public string LoanTypeName { get; set; } = null!;
    public int InstallmentId { get; set; }
    public int InstallmentNumber { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime PaymentDate { get; set; }
    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsLoanClosed { get; set; }
}
