using FluentAssertions;
using LoanManagement.Business.Abstract;
using LoanManagement.Business.ExternalServices;
using LoanManagement.Business.Services;
using LoanManagement.DataAccess.Context;
using LoanManagement.Entities.DTOs;
using LoanManagement.Entities.Enums;
using LoanManagement.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;

namespace LoanManagement.Tests.Services;

/// PaymentService'in dış sağlayıcı entegrasyonunu doğrular:
/// - Başarılı yanıt yeni PaymentResponseDto alanlarını doldurur
/// - Decline edilen yanıt PaymentDeclinedException fırlatır
/// - Idempotency key'i loan+installment'tan deterministic üretir
public class PaymentServiceExternalGatewayTests : IDisposable
{
    private readonly LoanDbContext _context;
    private readonly Mock<IExternalPaymentGatewayService> _gateway;

    public PaymentServiceExternalGatewayTests()
    {
        var options = new DbContextOptionsBuilder<LoanDbContext>()
            .UseInMemoryDatabase($"payment-svc-{Guid.NewGuid()}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new LoanDbContext(options);
        _gateway = new Mock<IExternalPaymentGatewayService>(MockBehavior.Strict);
    }

    public void Dispose() => _context.Dispose();

    private async Task<Loan> SeedLoanWithInstallmentsAsync(int loanId = 100, int customerId = 1, int totalInstallments = 3)
    {
        var customer = new Customer
        {
            Id = customerId,
            FirstName = "Ada",
            LastName = "Lovelace",
            IdentityNumber = "12345678901",
            Email = $"ada-{customerId}@test.com",
            PhoneNumber = "+905551112233",
        };
        _context.Customers.Add(customer);

        var loan = new Loan
        {
            Id = loanId,
            CustomerId = customerId,
            Amount = 30000m,
            Tenor = totalInstallments,
            ProfitRate = 1.5m,
            StartDate = new DateTime(2026, 1, 1),
            LoanType = LoanType.Personal,
            Status = LoanStatus.Active,
            Installments = new List<Installment>(),
        };
        for (int i = 1; i <= totalInstallments; i++)
        {
            loan.Installments!.Add(new Installment
            {
                Id = loanId * 10 + i,
                LoanId = loanId,
                InstallmentNumber = i,
                Amount = 10000m,
                DueDate = new DateTime(2026, i, 28),
                Status = InstallmentStatus.Unpaid,
            });
        }
        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();
        return loan;
    }

    [Fact]
    public async Task PayInstallmentAsync_OnGatewaySuccess_PopulatesNewStatusFields()
    {
        var loan = await SeedLoanWithInstallmentsAsync();
        _gateway
            .Setup(g => g.ChargeAsync(
                10000m,
                It.Is<string>(s => s.Contains("Kredi #100") && s.Contains("Taksit #1")),
                It.Is<string>(k => k == "loan-100-inst-1001"),
                It.IsAny<IReadOnlyDictionary<string, string>>()))
            .ReturnsAsync(new PaymentGatewayChargeResult
            {
                Status = PaymentGatewayStatus.Succeeded,
                ProviderName = "Stripe Sandbox",
                ProviderReference = "pi_test_first",
                Amount = 10000m,
                Currency = "TRY",
                ProcessedAtUtc = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc),
                Message = "ok",
            });

        var service = new PaymentService(_context, _gateway.Object);

        var result = await service.PayInstallmentAsync(new PaymentRequestDto { LoanId = loan.Id });

        result.Status.Should().Be("Succeeded");
        result.DeclineCode.Should().BeNull();
        result.ProviderName.Should().Be("Stripe Sandbox");
        result.ProviderReference.Should().Be("pi_test_first");
        result.InstallmentNumber.Should().Be(1);
        result.IsLoanClosed.Should().BeFalse();
        result.ProcessedAtUtc.Should().Be(new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc));

        // DB durumu kontrolü
        var refreshed = await _context.Loans
            .Include(l => l.Installments)!.ThenInclude(i => i.Payment)
            .FirstAsync(l => l.Id == loan.Id);
        refreshed.Installments!.Count(i => i.Status == InstallmentStatus.Paid).Should().Be(1);
        refreshed.Status.Should().Be(LoanStatus.Active);
    }

    [Fact]
    public async Task PayInstallmentAsync_WhenGatewayDeclines_ThrowsPaymentDeclinedException_AndDoesNotPersist()
    {
        var loan = await SeedLoanWithInstallmentsAsync(loanId: 200, customerId: 2);
        _gateway
            .Setup(g => g.ChargeAsync(It.IsAny<decimal>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
            .ReturnsAsync(new PaymentGatewayChargeResult
            {
                Status = PaymentGatewayStatus.Declined,
                ProviderName = "Stripe Sandbox",
                ProviderReference = string.Empty,
                DeclineCode = PaymentDeclineCodes.InsufficientFunds,
                Message = "Kart limiti yetersiz.",
                Amount = 10000m,
                Currency = "TRY",
            });

        var service = new PaymentService(_context, _gateway.Object);

        var act = async () => await service.PayInstallmentAsync(new PaymentRequestDto { LoanId = loan.Id });

        var ex = await act.Should().ThrowAsync<PaymentDeclinedException>();
        ex.Which.DeclineCode.Should().Be(PaymentDeclineCodes.InsufficientFunds);
        ex.Which.ProviderName.Should().Be("Stripe Sandbox");
        ex.Which.Status.Should().Be(PaymentGatewayStatus.Declined);
        ex.Which.Message.Should().Contain("limiti yetersiz");

        // Ödeme kaydı oluşturulmamış ve taksit hâlâ Unpaid olmalı
        (await _context.Payments.CountAsync()).Should().Be(0);
        var refreshed = await _context.Loans.Include(l => l.Installments).FirstAsync(l => l.Id == loan.Id);
        refreshed.Installments!.All(i => i.Status == InstallmentStatus.Unpaid).Should().BeTrue();
    }

    [Fact]
    public async Task PayInstallmentAsync_WhenAllInstallmentsPaid_ClosesLoan()
    {
        var loan = await SeedLoanWithInstallmentsAsync(loanId: 300, customerId: 3, totalInstallments: 2);
        // Önce ilk taksit ödenmiş olarak işaretle, sadece son taksit kaldı
        var first = loan.Installments!.First(i => i.InstallmentNumber == 1);
        first.Status = InstallmentStatus.Paid;
        _context.Payments.Add(new Payment
        {
            InstallmentId = first.Id,
            Amount = first.Amount,
            PaymentDate = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();

        _gateway
            .Setup(g => g.ChargeAsync(It.IsAny<decimal>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
            .ReturnsAsync(new PaymentGatewayChargeResult
            {
                Status = PaymentGatewayStatus.Succeeded,
                ProviderName = "Stripe Sandbox",
                ProviderReference = "pi_close",
                Amount = 10000m,
                Currency = "TRY",
            });

        var service = new PaymentService(_context, _gateway.Object);
        var result = await service.PayInstallmentAsync(new PaymentRequestDto { LoanId = loan.Id });

        result.IsLoanClosed.Should().BeTrue();
        var refreshed = await _context.Loans.FirstAsync(l => l.Id == loan.Id);
        refreshed.Status.Should().Be(LoanStatus.Closed);
    }

    [Fact]
    public async Task PayInstallmentAsync_WhenCustomerScopedToOtherLoan_ThrowsUnauthorized()
    {
        var loan = await SeedLoanWithInstallmentsAsync(loanId: 400, customerId: 4);
        var service = new PaymentService(_context, _gateway.Object);

        var act = async () => await service.PayInstallmentAsync(new PaymentRequestDto { LoanId = loan.Id }, scopedCustomerId: 999);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _gateway.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PayInstallmentAsync_WhenLoanMissing_ThrowsKeyNotFound()
    {
        var service = new PaymentService(_context, _gateway.Object);

        var act = async () => await service.PayInstallmentAsync(new PaymentRequestDto { LoanId = 9999 });

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _gateway.VerifyNoOtherCalls();
    }
}
