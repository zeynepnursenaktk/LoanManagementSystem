namespace LoanManagement.Business.Abstract;

/// Mock ödeme altyapısı servisi (Fake Payment Gateway).
/// Gerçek bir ödeme altyapısını simüle eder.
public interface IMockPaymentGatewayService
{
    /// Ödeme işlemini simüle eder.
    /// <param name="amount">Ödeme tutarı</param>
    /// <param name="description">Ödeme açıklaması</param>
    /// <returns>İşlem başarılıysa true</returns>
    Task<PaymentGatewayResult> ProcessPaymentAsync(decimal amount, string description);
}

public class PaymentGatewayResult
{
    public bool IsSuccess { get; set; }
    public string TransactionId { get; set; } = null!;
    public string Message { get; set; } = null!;
}
