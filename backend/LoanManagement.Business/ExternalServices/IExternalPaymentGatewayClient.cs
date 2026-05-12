namespace LoanManagement.Business.ExternalServices;

/// Dış ödeme ağ geçidi (Stripe Sandbox / iyzico Sandbox) ile iletişim soyutlaması.
public interface IExternalPaymentGatewayClient
{
    Task<PaymentGatewayChargeResult> ChargeAsync(
        PaymentGatewayChargeRequest request,
        CancellationToken cancellationToken = default);
}
