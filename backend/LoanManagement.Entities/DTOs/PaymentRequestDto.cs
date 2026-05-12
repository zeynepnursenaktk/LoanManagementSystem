using System.ComponentModel.DataAnnotations;

namespace LoanManagement.Entities.DTOs;

/// <summary>
/// Ödeme isteği: <see cref="LoanId"/> ile hangi kredinin ödeneceği belirlenir;
/// sistem o kredide sıradaki (1, 2, 3…) ödenmemiş taksi otomatik seçer.
/// </summary>
public class PaymentRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Kredi numarası (loanId) 0'dan büyük olmalıdır.")]
    public int LoanId { get; set; }
}
