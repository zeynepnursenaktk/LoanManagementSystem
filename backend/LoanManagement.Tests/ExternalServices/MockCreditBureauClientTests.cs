using System.Net;
using FluentAssertions;
using LoanManagement.Business.ExternalServices;
using LoanManagement.Tests.TestUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace LoanManagement.Tests.ExternalServices;

public class MockCreditBureauClientTests
{
    private static MockCreditBureauClient CreateClient(StubHttpMessageHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://findeks.test/api/v1/") };
        var options = Options.Create(new ExternalServicesOptions
        {
            CreditBureau = new CreditBureauOptions
            {
                ProviderName = "Findeks Test",
                ApiKey = "fk_test",
                BaseUrl = "https://findeks.test/api/v1/",
            },
        });
        return new MockCreditBureauClient(http, options, NullLogger<MockCreditBureauClient>.Instance);
    }

    [Fact]
    public async Task GetScoreAsync_OnSuccess_ParsesResponseAndIncludesProviderMetadata()
    {
        var handler = StubHttpMessageHandler.ReturnJson(HttpStatusCode.OK, """
            {
              "score": 1450,
              "riskLevel": "Low",
              "providerReference": "FNDX-20260512-000007",
              "queriedAtUtc": "2026-05-12T18:01:23Z"
            }
            """);
        var client = CreateClient(handler);

        var result = await client.GetScoreAsync(7);

        result.CustomerId.Should().Be(7);
        result.Score.Should().Be(1450);
        result.RiskLevel.Should().Be("Low");
        result.ProviderReference.Should().Be("FNDX-20260512-000007");
        result.ProviderName.Should().Be("Findeks Test");
        result.IsFresh.Should().BeTrue();
        result.Factors.Should().BeEmpty();

        handler.Requests.Single().RequestUri!.AbsolutePath.Should().EndWith("/credit-score/7");
    }

    [Fact]
    public async Task GetScoreAsync_ParsesFactorsArray_WhenSandboxIncludesBreakdown()
    {
        var handler = StubHttpMessageHandler.ReturnJson(HttpStatusCode.OK, """
            {
              "score": 1175,
              "riskLevel": "Low",
              "providerReference": "FNDX-x",
              "queriedAtUtc": "2026-05-12T18:01:23Z",
              "factors": [
                { "code": "base", "label": "Başlangıç skoru", "delta": 1000 },
                { "code": "closed_loan_bonus", "label": "1 kapatılmış kredi", "delta": 150 },
                { "code": "on_time_payment_bonus", "label": "1 ödenmiş taksit", "delta": 25 }
              ]
            }
            """);
        var client = CreateClient(handler);

        var result = await client.GetScoreAsync(1);

        result.Factors.Should().HaveCount(3);
        result.Factors.Should().ContainSingle(f => f.Code == "base" && f.Delta == 1000);
        result.Factors.Should().ContainSingle(f => f.Code == "closed_loan_bonus" && f.Delta == 150);
        result.Factors.Sum(f => f.Delta).Should().Be(result.Score);
    }

    [Fact]
    public async Task GetScoreAsync_FallbacksRiskLevel_FromScore()
    {
        var handler = StubHttpMessageHandler.ReturnJson(HttpStatusCode.OK, """
            { "score": 720, "providerReference": "FNDX-x" }
            """);
        var client = CreateClient(handler);

        var result = await client.GetScoreAsync(1);

        result.Score.Should().Be(720);
        result.RiskLevel.Should().Be("High");
    }

    [Fact]
    public async Task GetScoreAsync_OnNon2xx_Throws()
    {
        var handler = StubHttpMessageHandler.ReturnJson(HttpStatusCode.ServiceUnavailable, "{}");
        var client = CreateClient(handler);

        var act = async () => await client.GetScoreAsync(1);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Theory]
    [InlineData(1900, "VeryLow")]
    [InlineData(1500, "VeryLow")]
    [InlineData(1499, "Low")]
    [InlineData(1100, "Low")]
    [InlineData(1099, "Medium")]
    [InlineData(800, "Medium")]
    [InlineData(799, "High")]
    [InlineData(600, "High")]
    [InlineData(599, "VeryHigh")]
    [InlineData(300, "VeryHigh")]
    public void ClassifyRisk_BoundaryValues(int score, string expected)
    {
        MockCreditBureauClient.ClassifyRisk(score).Should().Be(expected);
    }
}
