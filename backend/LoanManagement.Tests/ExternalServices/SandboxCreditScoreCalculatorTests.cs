using FluentAssertions;
using LoanManagement.Business.ExternalServices;
using LoanManagement.Entities.Enums;
using LoanManagement.Entities.Models;
using Xunit;

namespace LoanManagement.Tests.ExternalServices;

/// SandboxCreditScoreCalculator'ın faktör başına etkilerini ve clamp davranışını doğrular.
public class SandboxCreditScoreCalculatorTests
{
    private static readonly DateTime Now = new(2026, 5, 12, 12, 0, 0, DateTimeKind.Utc);

    private static Loan ActiveLoan(int id, params (int dayOffset, InstallmentStatus status)[] installments)
    {
        var loan = new Loan
        {
            Id = id,
            CustomerId = 1,
            Status = LoanStatus.Active,
            Installments = new List<Installment>(),
        };
        int n = 1;
        foreach (var (offset, status) in installments)
        {
            loan.Installments!.Add(new Installment
            {
                Id = id * 100 + n,
                LoanId = id,
                InstallmentNumber = n++,
                Amount = 100m,
                DueDate = Now.AddDays(offset),
                Status = status,
            });
        }
        return loan;
    }

    private static Loan ClosedLoan(int id, int paidInstallmentCount = 12)
    {
        var loan = new Loan
        {
            Id = id,
            CustomerId = 1,
            Status = LoanStatus.Closed,
            Installments = new List<Installment>(),
        };
        for (int n = 1; n <= paidInstallmentCount; n++)
        {
            loan.Installments!.Add(new Installment
            {
                Id = id * 100 + n,
                LoanId = id,
                InstallmentNumber = n,
                Amount = 100m,
                DueDate = Now.AddDays(-30 * paidInstallmentCount),
                Status = InstallmentStatus.Paid,
            });
        }
        return loan;
    }

    [Fact]
    public void Calculate_WithNoLoans_AppliesNoHistoryPenalty()
    {
        var result = SandboxCreditScoreCalculator.Calculate(Array.Empty<Loan>(), Now);

        result.Score.Should().Be(SandboxCreditScoreCalculator.BaseScore - SandboxCreditScoreCalculator.NoHistoryPenalty);
        result.RiskLevel.Should().Be("Medium"); // 900
        result.Factors.Should().Contain(f => f.Code == "base" && f.Delta == 1000);
        result.Factors.Should().Contain(f => f.Code == "no_history_penalty" && f.Delta == -100);
    }

    [Fact]
    public void Calculate_WithSingleActiveLoanNoOverdue_AppliesActiveLoanPenaltyOnly()
    {
        var loans = new List<Loan>
        {
            ActiveLoan(1,
                (-30, InstallmentStatus.Paid),
                (+30, InstallmentStatus.Unpaid)),
        };

        var result = SandboxCreditScoreCalculator.Calculate(loans, Now);

        // 1000 - 50 (active) + 25 (1 paid)
        result.Score.Should().Be(1000 - 50 + 25);
        result.Factors.Should().Contain(f => f.Code == "active_loan_penalty" && f.Delta == -50);
        result.Factors.Should().Contain(f => f.Code == "on_time_payment_bonus" && f.Delta == 25);
        result.Factors.Should().NotContain(f => f.Code == "has_overdue_penalty");
        result.Factors.Should().NotContain(f => f.Code == "no_history_penalty");
    }

    [Fact]
    public void Calculate_WithClosedLoans_AppliesBonusAndPaidInstallmentBonusCapped()
    {
        // 3 kapatılmış kredi × 12 taksit = 36 ödenmiş taksit → bonus = 36*25=900 ama cap=500
        var loans = new List<Loan>
        {
            ClosedLoan(1),
            ClosedLoan(2),
            ClosedLoan(3),
        };

        var result = SandboxCreditScoreCalculator.Calculate(loans, Now);

        // 1000 + 3*150 (closed) + 500 (capped paid bonus) = 1950 → clamp 1900
        result.Score.Should().Be(SandboxCreditScoreCalculator.MaxScore);
        result.RiskLevel.Should().Be("VeryLow");

        result.Factors.Should().Contain(f => f.Code == "closed_loan_bonus" && f.Delta == 450);
        var onTime = result.Factors.Single(f => f.Code == "on_time_payment_bonus");
        onTime.Delta.Should().Be(SandboxCreditScoreCalculator.OnTimePaymentBonusCap);
        result.Factors.Should().Contain(f => f.Code == "clamp" && f.Delta < 0);
    }

    [Fact]
    public void Calculate_WithOverdueInstallment_AppliesBothCompoundingAndHasOverduePenalties()
    {
        var loans = new List<Loan>
        {
            ActiveLoan(1,
                (-60, InstallmentStatus.Unpaid),  // gecikmiş
                (-30, InstallmentStatus.Unpaid),  // gecikmiş
                (+30, InstallmentStatus.Unpaid)),
        };

        var result = SandboxCreditScoreCalculator.Calculate(loans, Now);

        // 1000 - 50 (active) - 2*10 (overdue compounding) - 300 (has_overdue) = 630
        result.Score.Should().Be(1000 - 50 - 20 - 300);
        result.RiskLevel.Should().Be("High");
        result.Factors.Should().Contain(f => f.Code == "overdue_installment_penalty" && f.Delta == -20);
        result.Factors.Should().Contain(f => f.Code == "has_overdue_penalty" && f.Delta == -300);
    }

    [Fact]
    public void Calculate_AppliesLowerBoundClamp_WhenScoreUnder300()
    {
        // 10 aktif kredi + 30 gecikmiş taksit → derin negatif, clamp 300'e
        var loans = new List<Loan>();
        for (int i = 1; i <= 10; i++)
        {
            var installments = Enumerable.Range(1, 3)
                .Select(_ => (-60, InstallmentStatus.Unpaid))
                .ToArray();
            loans.Add(ActiveLoan(i, installments));
        }

        var result = SandboxCreditScoreCalculator.Calculate(loans, Now);

        result.Score.Should().Be(SandboxCreditScoreCalculator.MinScore);
        result.RiskLevel.Should().Be("VeryHigh");
        result.Factors.Should().Contain(f => f.Code == "clamp" && f.Delta > 0);
    }

    [Fact]
    public void Calculate_FactorsSumEqualsClampedScore()
    {
        var loans = new List<Loan>
        {
            ClosedLoan(1, paidInstallmentCount: 6),
            ActiveLoan(2,
                (-30, InstallmentStatus.Paid),
                (+30, InstallmentStatus.Unpaid)),
        };

        var result = SandboxCreditScoreCalculator.Calculate(loans, Now);

        result.Factors.Sum(f => f.Delta).Should().Be(result.Score);
    }

    [Theory]
    [InlineData(1900, "VeryLow")]
    [InlineData(1500, "VeryLow")]
    [InlineData(1100, "Low")]
    [InlineData(800, "Medium")]
    [InlineData(600, "High")]
    [InlineData(599, "VeryHigh")]
    [InlineData(300, "VeryHigh")]
    public void ClassifyRisk_MatchesRiskBands(int score, string expected)
    {
        SandboxCreditScoreCalculator.ClassifyRisk(score).Should().Be(expected);
    }
}
