using LoanManagement.Business.Abstract;

namespace LoanManagement.Business.Services;

/// Mock ödeme altyapısı servisi.
/// Gerçek bir payment gateway'i (iyzico, PayTR vb.) simüle eder.
public class MockPaymentGatewayService : IMockPaymentGatewayService
{
    public async Task<PaymentGatewayResult> ProcessPaymentAsync(decimal amount, string description)
    {
        // Gerçek bir API çağrısını simüle etmek için kısa bir gecikme
        await Task.Delay(200);

        // %95 başarı oranı simülasyonu
        var random = new Random();
        bool isSuccess = random.Next(1, 101) <= 95;

        return new PaymentGatewayResult
        {
            IsSuccess = isSuccess,
            TransactionId = isSuccess ? Guid.NewGuid().ToString("N")[..12].ToUpper() : string.Empty,
            Message = isSuccess
                ? $"Ödeme başarılı. Tutar: {amount:C2}"
                : "Ödeme başarısız. Lütfen tekrar deneyiniz."
        };
    }
}
