using FluentAssertions;
using LoanManagement.Business.ExternalServices;
using LoanManagement.Business.Services;
using LoanManagement.DataAccess.Context;
using LoanManagement.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LoanManagement.Tests.Services;

public class ExternalCreditScoreServiceTests : IDisposable
{
    private readonly LoanDbContext _context;
    private readonly Mock<IExternalCreditBureauClient> _bureau;

    public ExternalCreditScoreServiceTests()
    {
        var options = new DbContextOptionsBuilder<LoanDbContext>()
            .UseInMemoryDatabase($"score-tests-{Guid.NewGuid()}")
            .Options;
        _context = new LoanDbContext(options);
        _bureau = new Mock<IExternalCreditBureauClient>(MockBehavior.Strict);
    }

    public void Dispose() => _context.Dispose();

    private ExternalCreditScoreService CreateSut()
        => new(_context, _bureau.Object, NullLogger<ExternalCreditScoreService>.Instance);

    private async Task SeedCustomerAsync(int id = 1, bool isDeleted = false)
    {
        _context.Customers.Add(new Customer
        {
            Id = id,
            FirstName = "Test",
            LastName = "User",
            IdentityNumber = "12345678901",
            Email = $"customer-{id}@test.com",
            PhoneNumber = "+905551112233",
            IsDeleted = isDeleted,
        });
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetDetailedScoreAsync_WhenCustomerExists_ReturnsBureauResult()
    {
        await SeedCustomerAsync(1);
        _bureau
            .Setup(b => b.GetScoreAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreditBureauScoreResult
            {
                CustomerId = 1,
                Score = 1450,
                RiskLevel = "Low",
                ProviderName = "Findeks Mock",
                ProviderReference = "FNDX-1",
                QueriedAtUtc = DateTime.UtcNow,
            });

        var result = await CreateSut().GetDetailedScoreAsync(1);

        result.Score.Should().Be(1450);
        result.RiskLevel.Should().Be("Low");
        result.ProviderName.Should().Be("Findeks Mock");
        _bureau.Verify(b => b.GetScoreAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDetailedScoreAsync_PropagatesFactorsFromBureau()
    {
        await SeedCustomerAsync(5);
        _bureau
            .Setup(b => b.GetScoreAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreditBureauScoreResult
            {
                CustomerId = 5,
                Score = 1175,
                RiskLevel = "Low",
                Factors = new[]
                {
                    new CreditScoreFactor("base", "Başlangıç skoru", 1000),
                    new CreditScoreFactor("closed_loan_bonus", "1 kapatılmış kredi", 150),
                    new CreditScoreFactor("on_time_payment_bonus", "1 ödenmiş taksit", 25),
                },
            });

        var result = await CreateSut().GetDetailedScoreAsync(5);

        result.Factors.Should().HaveCount(3);
        result.Factors.Sum(f => f.Delta).Should().Be(result.Score);
    }

    [Fact]
    public async Task GetCreditScoreAsync_LegacySurface_ReturnsIntScoreOnly()
    {
        await SeedCustomerAsync(2);
        _bureau
            .Setup(b => b.GetScoreAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreditBureauScoreResult { CustomerId = 2, Score = 880 });

        var score = await CreateSut().GetCreditScoreAsync(2);
        score.Should().Be(880);
    }

    [Fact]
    public async Task GetDetailedScoreAsync_WhenCustomerMissing_ThrowsKeyNotFound()
    {
        var act = async () => await CreateSut().GetDetailedScoreAsync(999);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");
        _bureau.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDetailedScoreAsync_WhenCustomerSoftDeleted_ThrowsKeyNotFound()
    {
        await SeedCustomerAsync(3, isDeleted: true);

        var act = async () => await CreateSut().GetDetailedScoreAsync(3);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _bureau.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDetailedScoreAsync_WhenBureauThrows_BubblesUpAsInvalidOperation()
    {
        await SeedCustomerAsync(4);
        _bureau
            .Setup(b => b.GetScoreAsync(4, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("downstream gone"));

        var act = async () => await CreateSut().GetDetailedScoreAsync(4);

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.InnerException.Should().BeOfType<HttpRequestException>();
    }
}
