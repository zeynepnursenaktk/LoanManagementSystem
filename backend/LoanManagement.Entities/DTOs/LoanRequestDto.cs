using System.ComponentModel.DataAnnotations;
using LoanManagement.Entities.Enums;

namespace LoanManagement.Entities.DTOs;

/// <summary>Kredi oluşturma isteği. <see cref="ProfitRate"/> yıllık kar oranıdır, yüzde cinsinden (örn. 24 = %24).</summary>
public class LoanRequestDto : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "Müşteri ID 0'dan büyük olmalıdır.")]
    public int CustomerId { get; set; }

    public decimal Amount { get; set; }

    [Range(1, 120, ErrorMessage = "Vade 1-120 ay arasında olmalıdır.")]
    public int Tenor { get; set; }

    /// <summary>Yıllık kar oranı, yüzde olarak (0–100). Örnek: 18,5 = yıllık %18,5.</summary>
    public decimal ProfitRate { get; set; }

    [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
    public DateTime StartDate { get; set; }

    [EnumDataType(typeof(LoanType), ErrorMessage = "Geçersiz kredi türü.")]
    public LoanType LoanType { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Amount <= 0)
        {
            yield return new ValidationResult(
                "Kredi tutarı 0'dan büyük olmalıdır.",
                new[] { nameof(Amount) });
        }

        if (Amount > 1_000_000_000m)
        {
            yield return new ValidationResult(
                "Kredi tutarı en fazla 1.000.000.000 olabilir.",
                new[] { nameof(Amount) });
        }

        if (ProfitRate < 0 || ProfitRate > 100)
        {
            yield return new ValidationResult(
                "Yıllık kar oranı 0 ile 100 arasında olmalıdır (yüzde; örn. 24 = %24).",
                new[] { nameof(ProfitRate) });
        }
    }
}
