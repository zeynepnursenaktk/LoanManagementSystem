using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LoanManagement.Business.ExternalServices;
using LoanManagement.Tests.TestUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace LoanManagement.Tests.ExternalServices;

/// External payment gateway client'ın (Stripe-uyumlu) HttpClient sözleşmesini doğrular.
/// MockHttpMessageHandler ile gerçek ağ olmadan tüm senaryoları kapsar.
public class StripeMockPaymentGatewayClientTests
{
    private const string ProviderName = "Stripe Sandbox Test";

    private static StripeMockPaymentGatewayClient CreateClient(StubHttpMessageHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.stripe.test/v1/") };
        var options = Options.Create(new ExternalServicesOptions
        {
            PaymentGateway = new PaymentGatewayOptions
            {
                ProviderName = ProviderName,
                ApiKey = "sk_test_unit",
                BaseUrl = "https://api.stripe.test/v1/",
                Currency = "TRY",
            },
        });
        return new StripeMockPaymentGatewayClient(http, options, NullLogger<StripeMockPaymentGatewayClient>.Instance);
    }

    private static PaymentGatewayChargeRequest SampleRequest(decimal amount = 100m) => new()
    {
        Amount = amount,
        Currency = "TRY",
        Description = "Test",
        IdempotencyKey = "test-key",
        Metadata = new Dictionary<string, string> { ["loanId"] = "10" },
    };

    [Fact]
    public async Task ChargeAsync_OnSucceededIntent_ReturnsSucceededResult()
    {
        var handler = StubHttpMessageHandler.ReturnJson(HttpStatusCode.OK, """
            {
              "id": "pi_test_OK",
              "object": "payment_intent",
              "status": "succeeded",
              "amount": 10000,
              "currency": "try"
            }
            """);
        var client = CreateClient(handler);

        var result = await client.ChargeAsync(SampleRequest());

        result.Status.Should().Be(PaymentGatewayStatus.Succeeded);
        result.IsSuccess.Should().BeTrue();
        result.ProviderName.Should().Be(ProviderName);
        result.ProviderReference.Should().Be("pi_test_OK");
        result.DeclineCode.Should().BeNull();
        result.Amount.Should().Be(100m);
        result.Currency.Should().Be("TRY");
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task ChargeAsync_SendsAuthorizationAndIdempotencyHeaders()
    {
        var handler = StubHttpMessageHandler.ReturnJson(HttpStatusCode.OK, """
            { "id": "pi_x", "status": "succeeded", "amount": 100, "currency": "try" }
            """);
        var client = CreateClient(handler);

        await client.ChargeAsync(SampleRequest());

        var sent = handler.Requests.Single();
        sent.Method.Should().Be(HttpMethod.Post);
        sent.RequestUri!.AbsolutePath.Should().EndWith("payment_intents");
        sent.Headers.Authorization!.Scheme.Should().Be("Bearer");
        sent.Headers.Authorization.Parameter.Should().Be("sk_test_unit");
        sent.Headers.GetValues("Idempotency-Key").Should().ContainSingle("test-key");
    }

    [Fact]
    public async Task ChargeAsync_On402WithDeclineCode_ReturnsDeclinedWithCode()
    {
        var handler = StubHttpMessageHandler.ReturnJson((HttpStatusCode)402, """
            {
              "error": {
                "code": "card_declined",
                "decline_code": "insufficient_funds",
                "message": "Kart limiti yetersiz."
              }
            }
            """);
        var client = CreateClient(handler);

        var result = await client.ChargeAsync(SampleRequest());

        result.Status.Should().Be(PaymentGatewayStatus.Declined);
        result.DeclineCode.Should().Be(PaymentDeclineCodes.InsufficientFunds);
        result.Message.Should().Be("Kart limiti yetersiz.");
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ChargeAsync_OnNetworkFailure_ReturnsFailedGatewayTimeout()
    {
        var handler = new StubHttpMessageHandler((_, _) => throw new HttpRequestException("connection refused"));
        var client = CreateClient(handler);

        var result = await client.ChargeAsync(SampleRequest());

        result.Status.Should().Be(PaymentGatewayStatus.Failed);
        result.DeclineCode.Should().Be(PaymentDeclineCodes.GatewayTimeout);
        result.Message.Should().Contain("ulaşılamadı");
    }

    [Fact]
    public async Task ChargeAsync_On500InternalError_ReturnsDeclinedProcessingError()
    {
        var handler = StubHttpMessageHandler.ReturnJson(HttpStatusCode.InternalServerError, """
            { "error": { "code": "processing_error", "message": "boom" } }
            """);
        var client = CreateClient(handler);

        var result = await client.ChargeAsync(SampleRequest());

        result.Status.Should().Be(PaymentGatewayStatus.Declined);
        result.DeclineCode.Should().Be(PaymentDeclineCodes.ProcessingError);
    }

    [Fact]
    public async Task ChargeAsync_ConvertsAmountToMinorUnits()
    {
        string? capturedBody = null;
        var handler = new StubHttpMessageHandler(async (req, ct) =>
        {
            capturedBody = req.Content is not null
                ? await req.Content.ReadAsStringAsync(ct)
                : null;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { id = "pi_ok", status = "succeeded", amount = 0, currency = "try" }),
            };
        });
        var client = CreateClient(handler);

        await client.ChargeAsync(SampleRequest(amount: 250.75m));

        capturedBody.Should().NotBeNull();
        capturedBody.Should().Contain("\"amount\":25075");
    }
}
