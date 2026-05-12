using System.ComponentModel.DataAnnotations;

namespace LoanManagement.Entities.Validation;

/// <summary>
/// Türkiye Cumhuriyeti Kimlik Numarası DataAnnotation kontrolü.
/// 11 hane + ilk hane 0 olamaz + Luhn-benzeri iki checksum hanesi (d10, d11).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class TurkishIdentityNumberAttribute : ValidationAttribute
{
    public TurkishIdentityNumberAttribute()
        : base("Geçerli bir T.C. Kimlik Numarası giriniz (11 haneli, ilk hane 0 olamaz, kontrol haneleri doğru olmalı).")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null) return false;
        return TurkishIdentityNumberValidator.IsValid(value.ToString());
    }
}
