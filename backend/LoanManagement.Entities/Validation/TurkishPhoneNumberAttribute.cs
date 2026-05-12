using System.ComponentModel.DataAnnotations;

namespace LoanManagement.Entities.Validation;

/// <summary>
/// Türkiye mobil telefon numarası DataAnnotation kontrolü.
/// Null/boş değer geçerli (opsiyonel alanlar için).
/// Beklenen format: 5xx xxx xx xx (10 hane) / 05xx xxx xx xx / +90 5xx xxx xx xx.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class TurkishPhoneNumberAttribute : ValidationAttribute
{
    public TurkishPhoneNumberAttribute()
        : base("Geçerli bir Türkiye GSM numarası girin. Örnek: 0532 123 45 67 veya +90 532 123 45 67.")
    {
    }

    public override bool IsValid(object? value)
        => TurkishPhoneNumberValidator.IsValid(value as string);
}
