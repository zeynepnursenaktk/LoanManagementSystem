using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using LoanManagement.Entities.DTOs;
using Xunit;

namespace LoanManagement.Tests.Validation;

public class RegisterRequestDtoValidationTests
{
    private static IList<ValidationResult> Validate(RegisterRequestDto dto)
    {
        var ctx = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, ctx, results, validateAllProperties: true);
        return results;
    }

    private static RegisterRequestDto ValidDto() => new()
    {
        Username = "ali.veli",
        Password = "Aa123456",
        FirstName = "Ali",
        LastName = "Veli",
        IdentityNumber = "12345678950",
        Email = "ali@example.com",
        PhoneNumber = "+905321234567"
    };

    [Fact]
    public void ValidDto_ProducesNoErrors()
    {
        Validate(ValidDto()).Should().BeEmpty();
    }

    [Theory]
    [InlineData("12345678901")] // checksum fail
    [InlineData("01234567895")] // ilk hane 0
    [InlineData("1234567890")]  // 10 hane
    [InlineData("12345A78950")] // harf
    public void InvalidIdentityNumber_ProducesError(string identity)
    {
        var dto = ValidDto();
        dto.IdentityNumber = identity;
        var errors = Validate(dto);
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(RegisterRequestDto.IdentityNumber)));
    }

    [Theory]
    [InlineData("4321234567")]
    [InlineData("532123456")]
    [InlineData("invalid")]
    [InlineData("+1 555 123 4567")]
    public void InvalidPhoneNumber_ProducesError(string phone)
    {
        var dto = ValidDto();
        dto.PhoneNumber = phone;
        var errors = Validate(dto);
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(RegisterRequestDto.PhoneNumber)));
    }

    [Fact]
    public void NullPhoneNumber_IsAccepted()
    {
        var dto = ValidDto();
        dto.PhoneNumber = null;
        Validate(dto).Should().BeEmpty();
    }

    [Theory]
    [InlineData("a")]                       // min 3
    [InlineData("ab")]                      // min 3
    [InlineData("kullanıcı adı boşluklu")]  // boşluk
    [InlineData("ad@min")]                  // @
    public void InvalidUsername_ProducesError(string username)
    {
        var dto = ValidDto();
        dto.Username = username;
        var errors = Validate(dto);
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(RegisterRequestDto.Username)));
    }

    [Theory]
    [InlineData("short")]      // 8 altı
    [InlineData("alllower1")]  // büyük harf yok
    [InlineData("ALLUPPER1")]  // küçük harf yok
    [InlineData("Password")]   // rakam yok
    public void WeakPassword_ProducesError(string password)
    {
        var dto = ValidDto();
        dto.Password = password;
        var errors = Validate(dto);
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(RegisterRequestDto.Password)));
    }

    [Theory]
    [InlineData("A")]                  // min 2
    [InlineData("Ali123")]             // rakam
    [InlineData("123Ali")]             // başlangıç rakam
    [InlineData("!!")]                 // sembol
    public void InvalidFirstName_ProducesError(string firstName)
    {
        var dto = ValidDto();
        dto.FirstName = firstName;
        var errors = Validate(dto);
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(RegisterRequestDto.FirstName)));
    }

    [Theory]
    [InlineData("Aliyev O'Connor")]    // kesme işareti ✓
    [InlineData("Demir-Doğan")]        // tire ✓
    [InlineData("Şükrü")]              // Türkçe karakter ✓
    public void ValidNameVariants_AreAccepted(string firstName)
    {
        var dto = ValidDto();
        dto.FirstName = firstName;
        var errors = Validate(dto);
        errors.Should().NotContain(e => e.MemberNames.Contains(nameof(RegisterRequestDto.FirstName)));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    public void InvalidEmail_ProducesError(string email)
    {
        var dto = ValidDto();
        dto.Email = email;
        var errors = Validate(dto);
        errors.Should().Contain(e => e.MemberNames.Contains(nameof(RegisterRequestDto.Email)));
    }
}
