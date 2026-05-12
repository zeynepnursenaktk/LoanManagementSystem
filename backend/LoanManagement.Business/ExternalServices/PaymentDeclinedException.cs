namespace LoanManagement.Business.ExternalServices;

/// Dış ödeme sağlayıcısı işlemi reddettiğinde fırlatılır.
/// Controller tarafı bu exception'ı 402 Payment Required yanıtına dönüştürür.
public sealed class PaymentDeclinedException : Exception
{
    public string DeclineCode { get; }
    public string ProviderName { get; }
    public PaymentGatewayStatus Status { get; }

    public PaymentDeclinedException(
        string declineCode,
        string providerName,
        string message,
        PaymentGatewayStatus status = PaymentGatewayStatus.Declined)
        : base(message)
    {
        DeclineCode = declineCode;
        ProviderName = providerName;
        Status = status;
    }
}
