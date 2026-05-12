using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using LoanManagement.Entities.DTOs;
using Xunit;

namespace LoanManagement.Tests.Validation;

public class CreateCustomerDtoValidationTests
{
    private static IList<ValidationResult> Validate(CreateCustomerDto dto)
    {
        var ctx = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, ctx, results, validateAllProperties: true);
        return results;
    }

    private static CreateCustomerDto ValidDto() => new()
    {
        FirstName = "Ayşe",
        LastName = "Yılmaz",
        IdentityNumber = "12345678950",
        Email = "ayse@example.com",
        PhoneNumber = "0532 123 45 67"
    };

    [Fact]
    public void ValidDto_NoErrors()
    {
        Validate(ValidDto()).Should().BeEmpty();
    }

    [Fact]
    public void NullPhone_NoErrors()
    {
        var dto = ValidDto();
        dto.PhoneNumber = null;
        Validate(dto).Should().BeEmpty();
    }

    [Theory]
    [InlineData("11111111111")] // invalid checksum
    [InlineData("00000000000")] // 0 başlangıç
    [InlineData("abcd1234567")] // harf
    public void InvalidIdentity_ProducesError(string id)
    {
        var dto = ValidDto();
        dto.IdentityNumber = id;
        Validate(dto).Should().Contain(e => e.MemberNames.Contains(nameof(CreateCustomerDto.IdentityNumber)));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("abc")]
    [InlineData("+1 555 555 5555")]
    public void InvalidPhone_ProducesError(string phone)
    {
        var dto = ValidDto();
        dto.PhoneNumber = phone;
        Validate(dto).Should().Contain(e => e.MemberNames.Contains(nameof(CreateCustomerDto.PhoneNumber)));
    }
}
