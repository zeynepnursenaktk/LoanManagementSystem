using LoanManagement.Business.ExternalServices;

namespace LoanManagement.Business.Abstract;

/// Mevcut `IMockPaymentGatewayService`'i genişletir: yalnızca `IsSuccess` değil,
/// decline kodu / provider reference / message gibi zengin yanıt sağlar.
public interface IExternalPaymentGatewayService
{
    Task<PaymentGatewayChargeResult> ChargeAsync(
        decimal amount,
        string description,
        string? idempotencyKey,
        IReadOnlyDictionary<string, string>? metadata);
}
