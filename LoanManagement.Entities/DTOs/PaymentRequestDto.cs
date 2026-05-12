namespace LoanManagement.Entities.DTOs;

/// Ödeme isteği: Belirli bir taksit ID'si ile ödeme yapılır.
/// Bir ödeme yalnızca tek bir takside ait olabilir.
public class PaymentRequestDto
{
    public int InstallmentId { get; set; }
}
