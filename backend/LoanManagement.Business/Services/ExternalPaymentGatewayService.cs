using LoanManagement.Business.Abstract;
using LoanManagement.Business.ExternalServices;

namespace LoanManagement.Business.Services;

/// Mevcut `IMockPaymentGatewayService` sözleşmesini koruyan adapter.
/// Asıl iş `IExternalPaymentGatewayClient` (Stripe Sandbox) tarafından yapılır;
/// adapter, sonucu `PaymentService`'in beklediği <see cref="PaymentGatewayResult"/>'a normalize eder.
public sealed class ExternalPaymentGatewayService : IMockPaymentGatewayService, IExternalPaymentGatewayService
{
    private readonly IExternalPaymentGatewayClient _client;

    public ExternalPaymentGatewayService(IExternalPaymentGatewayClient client)
    {
        _client = client;
    }

    public async Task<PaymentGatewayResult> ProcessPaymentAsync(decimal amount, string description)
    {
        var detailed = await ChargeAsync(amount, description, idempotencyKey: null, metadata: null)
            .ConfigureAwait(false);

        return new PaymentGatewayResult
        {
            IsSuccess = detailed.IsSuccess,
            TransactionId = detailed.IsSuccess ? detailed.ProviderReference : string.Empty,
            Message = detailed.Message,
        };
    }

    public async Task<PaymentGatewayChargeResult> ChargeAsync(
        decimal amount,
        string description,
        string? idempotencyKey,
        IReadOnlyDictionary<string, string>? metadata)
    {
        var request = new PaymentGatewayChargeRequest
        {
            Amount = amount,
            Currency = "TRY",
            Description = description,
            IdempotencyKey = idempotencyKey ?? Guid.NewGuid().ToString("N"),
            Metadata = metadata ?? new Dictionary<string, string>(),
        };
        return await _client.ChargeAsync(request).ConfigureAwait(false);
    }
}
