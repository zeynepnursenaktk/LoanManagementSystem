using System.Text.RegularExpressions;

namespace LoanManagement.Entities.Validation;

/// <summary>
/// Türkiye GSM telefon numarası doğrulayıcısı.
///
/// Kabul edilen formatlar (boşluk/tire/parantez/nokta tolere edilir; kontrol normalize sonrası yapılır):
///   5xx xxx xx xx          → 10 hane
///   05xx xxx xx xx         → 11 hane (0 ile)
///   +90 5xx xxx xx xx      → 12 hane (+90 ile)
///   90 5xx xxx xx xx       → 12 hane (90 ile)
///
/// Yalnızca **mobil** GSM (operatör kodu 5 ile başlamak zorunda) kabul edilir;
/// bankacılık platformu OTP gönderebilmek için cep telefonu tercih eder.
///
/// İsteğe bağlı (null/empty) değer geçerli sayılır — opsiyonel alan boş kalabilir.
/// </summary>
public static class TurkishPhoneNumberValidator
{
    // Normalize sonrası tek pattern: opsiyonel +90 / 90 / 0 ön eki + 5XXXXXXXXX
    private static readonly Regex NormalizedPattern = new(
        @"^(\+90|90|0)?5[0-9]{9}$",
        RegexOptions.Compiled);

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true; // null/boş → opsiyonel kabul

        var normalized = Normalize(value!);

        // Kullanıcı bir şey yazmış ama normalize sonrası geçerli rakam kalmamışsa
        // (ör. "abc", "---", "(...)" gibi)  → açıkça geçersiz.
        if (normalized.Length == 0) return false;

        return NormalizedPattern.IsMatch(normalized);
    }

    /// Boşluk, tire, parantez ve nokta gibi formatlama karakterlerini temizler.
    /// Sonuçta opsiyonel "+" ön eki kalır; geri kalan yalnızca rakamdır.
    public static string Normalize(string value)
    {
        var sb = new System.Text.StringBuilder(value.Length);
        foreach (var c in value.Trim())
        {
            if (c == '+' && sb.Length == 0)
            {
                sb.Append('+');
            }
            else if (char.IsDigit(c))
            {
                sb.Append(c);
            }
            // diğer karakterler atlanır
        }
        return sb.ToString();
    }
}
