namespace LoanManagement.Entities.Validation;

/// <summary>
/// Türkiye Cumhuriyeti Kimlik Numarası doğrulama yardımcısı.
///
/// Resmî algoritma (NVI):
///  1. 11 haneli, sadece sayı.
///  2. İlk hane 0 olamaz.
///  3. 10. hane = ((d1+d3+d5+d7+d9) * 7 - (d2+d4+d6+d8)) mod 10.
///  4. 11. hane = (d1+d2+...+d10) mod 10.
/// </summary>
public static class TurkishIdentityNumberValidator
{
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;

        var s = value.Trim();
        if (s.Length != 11) return false;

        if (s[0] == '0') return false;

        var digits = new int[11];
        for (int i = 0; i < 11; i++)
        {
            if (!char.IsDigit(s[i])) return false;
            digits[i] = s[i] - '0';
        }

        int oddSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        int evenSum = digits[1] + digits[3] + digits[5] + digits[7];
        int d10 = ((oddSum * 7) - evenSum) % 10;
        if (d10 < 0) d10 += 10;
        if (d10 != digits[9]) return false;

        int totalSum = 0;
        for (int i = 0; i < 10; i++) totalSum += digits[i];
        int d11 = totalSum % 10;
        if (d11 != digits[10]) return false;

        return true;
    }
}
