using LoanManagement.Entities.DTOs;

namespace LoanManagement.Business.Abstract;

public interface IPaymentService
{
    /// <param name="scopedCustomerId">Müşteri rolü için zorunlu: <see cref="PaymentRequestDto.LoanId"/> kredisinin müşteri ID'si ile eşleşmeli.</param>
    Task<PaymentResponseDto> PayInstallmentAsync(PaymentRequestDto request, int? scopedCustomerId = null);

    // Tüm ödemeleri listeler.
    Task<List<PaymentResponseDto>> GetAllPaymentsAsync();

    // Müşteri bazlı ödemeleri getirir.
    Task<List<PaymentResponseDto>> GetPaymentsByCustomerIdAsync(int customerId);
}
