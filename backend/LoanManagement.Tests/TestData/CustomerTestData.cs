using LoanManagement.Entities.DTOs;

namespace LoanManagement.Tests.TestData;

/// <summary>
/// Müşteri test datası - Builder pattern ile oluşturma
/// </summary>
public class CustomerTestDataBuilder
{
    private string _firstName = "Ahmet";
    private string _lastName = "Yılmaz";
    private string _identityNumber = "12345678901";
    private string _email = "ahmet@example.com";
    private string? _phoneNumber = "+905551234567";

    public CustomerTestDataBuilder WithFirstName(string firstName)
    {
        _firstName = firstName;
        return this;
    }

    public CustomerTestDataBuilder WithLastName(string lastName)
    {
        _lastName = lastName;
        return this;
    }

    public CustomerTestDataBuilder WithIdentityNumber(string identityNumber)
    {
        _identityNumber = identityNumber;
        return this;
    }

    public CustomerTestDataBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public CustomerTestDataBuilder WithPhoneNumber(string? phoneNumber)
    {
        _phoneNumber = phoneNumber;
        return this;
    }

    public CreateCustomerDto BuildCreateDto()
    {
        return new CreateCustomerDto
        {
            FirstName = _firstName,
            LastName = _lastName,
            IdentityNumber = _identityNumber,
            Email = _email,
            PhoneNumber = _phoneNumber
        };
    }

    public UpdateCustomerDto BuildUpdateDto()
    {
        return new UpdateCustomerDto
        {
            Email = _email,
            PhoneNumber = _phoneNumber
        };
    }
}

/// <summary>
/// Mock DTO'lar - Varsayılan test datası oluştur
/// </summary>
public static class CustomerTestData
{
    public static CreateCustomerDto ValidCreateCustomerDto()
    {
        return new CustomerTestDataBuilder()
            .WithFirstName("John")
            .WithLastName("Doe")
            .WithIdentityNumber("98765432101")
            .WithEmail("john.doe@example.com")
            .WithPhoneNumber("+905559876543")
            .BuildCreateDto();
    }

    public static CreateCustomerDto ValidCreateCustomerDto(string email, string identityNumber)
    {
        return new CustomerTestDataBuilder()
            .WithFirstName("Jane")
            .WithLastName("Smith")
            .WithIdentityNumber(identityNumber)
            .WithEmail(email)
            .WithPhoneNumber("+905555555555")
            .BuildCreateDto();
    }

    public static CreateCustomerDto InvalidFirstName()
    {
        return new CustomerTestDataBuilder()
            .WithFirstName("") // Boş ad
            .WithLastName("Doe")
            .WithIdentityNumber("11111111111")
            .WithEmail("invalid1@example.com")
            .BuildCreateDto();
    }

    public static CreateCustomerDto InvalidLastName()
    {
        return new CustomerTestDataBuilder()
            .WithFirstName("John")
            .WithLastName("") // Boş soyad
            .WithIdentityNumber("11111111112")
            .WithEmail("invalid2@example.com")
            .BuildCreateDto();
    }

    public static CreateCustomerDto InvalidEmail()
    {
        return new CustomerTestDataBuilder()
            .WithFirstName("John")
            .WithLastName("Doe")
            .WithIdentityNumber("11111111113")
            .WithEmail("invalid-email") // Geçersiz email
            .BuildCreateDto();
    }

    public static CreateCustomerDto InvalidPhoneNumber()
    {
        return new CustomerTestDataBuilder()
            .WithFirstName("John")
            .WithLastName("Doe")
            .WithIdentityNumber("11111111114")
            .WithEmail("valid@example.com")
            .WithPhoneNumber("invalid-phone")
            .BuildCreateDto();
    }

    public static CreateCustomerDto InvalidIdentityNumber()
    {
        return new CustomerTestDataBuilder()
            .WithFirstName("John")
            .WithLastName("Doe")
            .WithIdentityNumber("123") // Geçersiz TCNo (11 hane değil)
            .WithEmail("valid123@example.com")
            .BuildCreateDto();
    }

    public static CreateCustomerDto InvalidIdentityNumberFormat()
    {
        return new CustomerTestDataBuilder()
            .WithFirstName("John")
            .WithLastName("Doe")
            .WithIdentityNumber("1234567890A") // Harf içeriyor
            .WithEmail("valid456@example.com")
            .BuildCreateDto();
    }

    public static CreateCustomerDto NameTooLong()
    {
        var longName = new string('A', 101); // 101 karakter (max: 100)
        return new CustomerTestDataBuilder()
            .WithFirstName(longName)
            .WithLastName("Doe")
            .WithIdentityNumber("11111111115")
            .WithEmail("valid789@example.com")
            .BuildCreateDto();
    }

    public static CreateCustomerDto FirstNameTooShort()
    {
        return new CustomerTestDataBuilder()
            .WithFirstName("A") // Min: 2 karakter
            .WithLastName("Doe")
            .WithIdentityNumber("11111111116")
            .WithEmail("valid000@example.com")
            .BuildCreateDto();
    }

    public static UpdateCustomerDto ValidUpdateDto()
    {
        return new CustomerTestDataBuilder()
            .WithEmail("updated@example.com")
            .WithPhoneNumber("+905551111111")
            .BuildUpdateDto();
    }

    public static UpdateCustomerDto InvalidEmailUpdate()
    {
        return new CustomerTestDataBuilder()
            .WithEmail("invalid-email")
            .BuildUpdateDto();
    }

    public static UpdateCustomerDto InvalidPhoneNumberUpdate()
    {
        return new CustomerTestDataBuilder()
            .WithEmail("valid@example.com")
            .WithPhoneNumber("invalid-phone")
            .BuildUpdateDto();
    }

    public static CustomerListDto GetValidCustomerListDto(int id = 1)
    {
        return new CustomerListDto
        {
            Id = id,
            FirstName = "Test",
            LastName = "Customer",
            Email = $"customer{id}@example.com",
            PhoneNumber = "+905551234567",
            TotalLoans = 0,
            Loans = new List<LoanSummaryDto>()
        };
    }

    public static CustomerResponseDto GetValidCustomerResponseDto(int id = 1)
    {
        return new CustomerResponseDto
        {
            Id = id,
            FirstName = "Test",
            LastName = "Customer",
            Email = $"customer{id}@example.com",
            PhoneNumber = "+905551234567",
            IdentityNumber = "12345678901",
            TotalLoans = 0,
            Loans = new List<LoanSummaryDto>()
        };
    }

    public static CustomerSummaryDto GetValidCustomerSummaryDto(int id = 1)
    {
        return new CustomerSummaryDto
        {
            CustomerId = id,
            FullName = "Test Customer",
            TotalLoans = 2,
            ActiveLoans = 1,
            ClosedLoans = 1,
            TotalDebt = 50000m,
            TotalPaid = 10000m,
            TotalInstallments = 24,
            PaidInstallments = 5,
            UnpaidInstallments = 19,
            OverdueInstallments = 0,
            Loans = new List<LoanDetailSummaryDto>()
        };
    }
}
