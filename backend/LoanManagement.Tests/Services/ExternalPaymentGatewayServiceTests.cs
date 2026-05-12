using FluentAssertions;
using LoanManagement.Business.Abstract;
using LoanManagement.Business.ExternalServices;
using LoanManagement.Business.Services;
using Moq;
using Xunit;

namespace LoanManagement.Tests.Services;

public class ExternalPaymentGatewayServiceTests
{
    private readonly Mock<IExternalPaymentGatewayClient> _client = new(MockBehavior.Strict);

    private ExternalPaymentGatewayService CreateSut() => new(_client.Object);

    [Fact]
    public async Task ChargeAsync_OnSuccess_ReturnsSucceededResult()
    {
        _client.Setup(c => c.ChargeAsync(It.IsAny<PaymentGatewayChargeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentGatewayChargeResult
            {
                Status = PaymentGatewayStatus.Succeeded,
                ProviderName = "Stripe Sandbox",
                ProviderReference = "pi_test_OK",
                Message = "ok",
                Amount = 100m,
                Currency = "TRY",
            });

        var sut = CreateSut();
        var metadata = new Dictionary<string, string> { ["loanId"] = "10" };

        var result = await sut.ChargeAsync(100m, "Taksit #1", "idk-1", metadata);

        result.IsSuccess.Should().BeTrue();
        result.ProviderReference.Should().Be("pi_test_OK");
        _client.Verify(c => c.ChargeAsync(
            It.Is<PaymentGatewayChargeRequest>(r =>
                r.Amount == 100m && r.IdempotencyKey == "idk-1" && r.Metadata["loanId"] == "10"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LegacyProcessPaymentAsync_OnDecline_MapsToIsSuccessFalse()
    {
        _client.Setup(c => c.ChargeAsync(It.IsAny<PaymentGatewayChargeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentGatewayChargeResult
            {
                Status = PaymentGatewayStatus.Declined,
                ProviderName = "Stripe Sandbox",
                ProviderReference = string.Empty,
                DeclineCode = PaymentDeclineCodes.InsufficientFunds,
                Message = "Kart limiti yetersiz.",
                Amount = 50m,
                Currency = "TRY",
            });

        IMockPaymentGatewayService sut = CreateSut();

        var legacy = await sut.ProcessPaymentAsync(50m, "Taksit #2");

        legacy.IsSuccess.Should().BeFalse();
        legacy.TransactionId.Should().BeEmpty();
        legacy.Message.Should().Contain("limiti yetersiz");
    }

    [Fact]
    public async Task ChargeAsync_GeneratesIdempotencyKey_WhenNullProvided()
    {
        PaymentGatewayChargeRequest? captured = null;
        _client.Setup(c => c.ChargeAsync(It.IsAny<PaymentGatewayChargeRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PaymentGatewayChargeRequest, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync(new PaymentGatewayChargeResult
            {
                Status = PaymentGatewayStatus.Succeeded,
                ProviderName = "x",
                ProviderReference = "pi",
                Amount = 1m,
            });

        await CreateSut().ChargeAsync(1m, "x", idempotencyKey: null, metadata: null);

        captured.Should().NotBeNull();
        captured!.IdempotencyKey.Should().NotBeNullOrWhiteSpace();
        captured.Metadata.Should().NotBeNull();
    }
}
