using LoanManagement.Entities.DTOs;

namespace LoanManagement.Business.Abstract;

public interface IPaymentService
{
    // Belirli bir taksit için ödeme yapar. Aynı taksit iki kere ödenemez.
    Task<PaymentResponseDto> PayInstallmentAsync(PaymentRequestDto request);

    // Tüm ödemeleri listeler.
    Task<List<PaymentResponseDto>> GetAllPaymentsAsync();

    // Müşteri bazlı ödemeleri getirir.
    Task<List<PaymentResponseDto>> GetPaymentsByCustomerIdAsync(int customerId);
}
