using FluentAssertions;
using LoanManagement.Entities.Validation;
using Xunit;

namespace LoanManagement.Tests.Validation;

public class TurkishIdentityNumberValidatorTests
{
    [Theory]
    [InlineData("12345678950")] // d10=5, d11=0
    [InlineData("98765432150")] // d10=5, d11=0
    [InlineData("11111111110")] // tüm 1'ler + check
    [InlineData("22222222220")]
    [InlineData("33333333330")]
    [InlineData("10000000146")]
    public void IsValid_WithKnownValidNumbers_ReturnsTrue(string identity)
    {
        TurkishIdentityNumberValidator.IsValid(identity).Should().BeTrue();
    }

    [Theory]
    [InlineData("12345678901")] // d10 yanlış
    [InlineData("11111111111")] // d11 yanlış
    [InlineData("98765432100")] // d10 yanlış
    [InlineData("98765432101")] // d11 yanlış
    public void IsValid_WithFailedChecksum_ReturnsFalse(string identity)
    {
        TurkishIdentityNumberValidator.IsValid(identity).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1234567890")]      // 10 hane
    [InlineData("123456789012")]    // 12 hane
    [InlineData("01234567895")]     // ilk hane 0
    [InlineData("12345A78950")]     // harf
    [InlineData("1234 678950")]     // boşluk
    public void IsValid_WithMalformedInput_ReturnsFalse(string? identity)
    {
        TurkishIdentityNumberValidator.IsValid(identity).Should().BeFalse();
    }

    [Fact]
    public void IsValid_TolaratesSurroundingWhitespace()
    {
        TurkishIdentityNumberValidator.IsValid("  12345678950  ").Should().BeTrue();
    }
}
