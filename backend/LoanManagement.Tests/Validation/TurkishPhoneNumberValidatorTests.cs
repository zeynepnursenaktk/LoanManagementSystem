using FluentAssertions;
using LoanManagement.Entities.Validation;
using Xunit;

namespace LoanManagement.Tests.Validation;

public class TurkishPhoneNumberValidatorTests
{
    [Theory]
    [InlineData("5321234567")]              // 10 hane
    [InlineData("05321234567")]             // 11 hane, 0 ile
    [InlineData("+905321234567")]           // +90 ile
    [InlineData("905321234567")]            // 90 ile
    [InlineData("+90 532 123 45 67")]       // boşluklu
    [InlineData("+90 (532) 123-45-67")]     // parantez/tire
    [InlineData("0532 123 45 67")]
    [InlineData("532.123.45.67")]
    public void IsValid_WithProperMobileNumbers_ReturnsTrue(string phone)
    {
        TurkishPhoneNumberValidator.IsValid(phone).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_WithNullOrEmpty_ReturnsTrue(string? phone)
    {
        // Opsiyonel alan: boş kabul edilir.
        TurkishPhoneNumberValidator.IsValid(phone).Should().BeTrue();
    }

    [Theory]
    [InlineData("4321234567")]        // 4 ile başlıyor (mobil değil)
    [InlineData("2121234567")]        // sabit hat
    [InlineData("532123456")]         // 9 hane (eksik)
    [InlineData("53212345678")]       // 11 hane (fazla)
    [InlineData("+9053212345")]       // toplam yetersiz
    [InlineData("invalid-phone")]
    [InlineData("abc")]
    [InlineData("0000000000")]
    [InlineData("+1 555 123 45 67")]  // ABD kodu
    [InlineData("00905321234567")]    // çift sıfır prefix
    public void IsValid_WithInvalidNumbers_ReturnsFalse(string phone)
    {
        TurkishPhoneNumberValidator.IsValid(phone).Should().BeFalse();
    }

    [Theory]
    [InlineData("0532 123 45 67", "05321234567")]
    [InlineData("+90 (532) 123-45-67", "+905321234567")]
    [InlineData("  +90 532 123 45 67  ", "+905321234567")]
    public void Normalize_StripsFormattingCharacters(string input, string expected)
    {
        TurkishPhoneNumberValidator.Normalize(input).Should().Be(expected);
    }
}
