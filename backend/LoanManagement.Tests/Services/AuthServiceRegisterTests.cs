using FluentAssertions;
using LoanManagement.Business.Services;
using LoanManagement.DataAccess.Context;
using LoanManagement.Entities;
using LoanManagement.Entities.DTOs;
using LoanManagement.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LoanManagement.Tests.Services;

/// <summary>
/// AuthService.RegisterAsync için defensive validation + normalization testleri.
/// Servis katmanı, controller bypass edilse bile DTO içeriğini doğrular
/// ve verileri tutarlı (trim, lowercase, normalize edilmiş telefon) saklar.
/// </summary>
public class AuthServiceRegisterTests : IDisposable
{
    private readonly LoanDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthServiceRegisterTests()
    {
        var options = new DbContextOptionsBuilder<LoanDbContext>()
            .UseInMemoryDatabase($"auth-tests-{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new LoanDbContext(options);

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TestKey-LongEnoughForHmac256-SuperSecret-1234567890",
                ["Jwt:Issuer"] = "Tests",
                ["Jwt:Audience"] = "Tests"
            })
            .Build();
    }

    public void Dispose() => _context.Dispose();

    private AuthService CreateSut() => new(_context, _configuration);

    private static RegisterRequestDto ValidRequest() => new()
    {
        Username = "yeni.kullanici",
        Password = "Aa123456",
        FirstName = "Yeni",
        LastName = "Kullanıcı",
        IdentityNumber = "12345678950",
        Email = "Yeni.User@Example.COM",
        PhoneNumber = "+90 532 123 45 67"
    };

    [Fact]
    public async Task RegisterAsync_WithValidRequest_PersistsNormalizedCustomerAndUser()
    {
        var sut = CreateSut();

        var token = await sut.RegisterAsync(ValidRequest());

        token.Token.Should().NotBeNullOrWhiteSpace();
        token.Role.Should().Be(AppRoles.Customer);

        var customer = await _context.Customers.SingleAsync();
        customer.FirstName.Should().Be("Yeni");
        customer.LastName.Should().Be("Kullanıcı");
        customer.IdentityNumber.Should().Be("12345678950");
        customer.Email.Should().Be("yeni.user@example.com"); // normalize edilmiş
        customer.PhoneNumber.Should().Be("+905321234567");   // boşluk/karakter temizlenmiş

        var user = await _context.Users.SingleAsync();
        user.Username.Should().Be("yeni.kullanici");
        user.Role.Should().Be(AppRoles.Customer);
        user.CustomerId.Should().Be(customer.Id);
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidIdentity_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var req = ValidRequest();
        req.IdentityNumber = "12345678901"; // checksum hatası

        Func<Task> act = () => sut.RegisterAsync(req);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*T.C. Kimlik*");

        _context.Customers.Should().BeEmpty();
        _context.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidPhone_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var req = ValidRequest();
        req.PhoneNumber = "abc"; // geçersiz

        Func<Task> act = () => sut.RegisterAsync(req);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*telefon*");

        _context.Customers.Should().BeEmpty();
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateUsername_ThrowsInvalidOperationException()
    {
        _context.Users.Add(new User
        {
            Username = "yeni.kullanici",
            PasswordHash = "x",
            FullName = "Mevcut",
            Role = AppRoles.Customer,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var sut = CreateSut();

        Func<Task> act = () => sut.RegisterAsync(ValidRequest());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*kullanıcı adı*");
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateIdentity_ThrowsInvalidOperationException()
    {
        _context.Customers.Add(new Customer
        {
            FirstName = "Var",
            LastName = "Olan",
            IdentityNumber = "12345678950",
            Email = "var@example.com",
            PhoneNumber = "05551112233"
        });
        await _context.SaveChangesAsync();

        var sut = CreateSut();

        Func<Task> act = () => sut.RegisterAsync(ValidRequest());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*TC*");
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmailDifferentCasing_DetectsCollision()
    {
        // Mevcut müşterinin e-postası lowercase kaydedilmiştir.
        _context.Customers.Add(new Customer
        {
            FirstName = "Var",
            LastName = "Olan",
            IdentityNumber = "22222222220",
            Email = "yeni.user@example.com",
            PhoneNumber = "05551112233"
        });
        await _context.SaveChangesAsync();

        var sut = CreateSut();
        // Yeni kayıt büyük harfli versiyon dener; normalize sonrası çakışmalı.
        var req = ValidRequest();
        req.Email = "Yeni.USER@Example.COM";

        Func<Task> act = () => sut.RegisterAsync(req);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*e-posta*");
    }

    [Fact]
    public async Task RegisterAsync_TrimsWhitespaceOnNames()
    {
        var sut = CreateSut();
        var req = ValidRequest();
        req.FirstName = "  Yeni  ";
        req.LastName = "  Kullanıcı  ";

        await sut.RegisterAsync(req);

        var customer = await _context.Customers.SingleAsync();
        customer.FirstName.Should().Be("Yeni");
        customer.LastName.Should().Be("Kullanıcı");
    }
}
