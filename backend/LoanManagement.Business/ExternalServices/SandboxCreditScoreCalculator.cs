using LoanManagement.Entities.Enums;
using LoanManagement.Entities.Models;

namespace LoanManagement.Business.ExternalServices;

/// Sandbox kredi skoru hesaplama motoru.
/// Müşterinin kredi geçmişine (loans + installments + payments) bakarak deterministic
/// bir skor ve "neden bu skor?" sorusuna yanıt veren faktör listesi üretir.
///
/// FORMÜL (toplam: aşağıdakilerin clamp'lenmiş hâli, [300, 1900]):
/// - Başlangıç: +1000
/// - Kapatılmış her kredi: +150 (bonus)
/// - Aktif her kredi: -50 (yük)
/// - En az bir gecikmiş taksit varsa: -300 (büyük risk sinyali)
/// - Zamanında ödenmiş her taksit: +25 (sadakat / disiplin), toplam max +500
/// - Gecikmiş her taksit: -10 (compounding penalty)
/// - Hiç kredi geçmişi yoksa: -100 (thin file)
///
/// Hesaplama saf statik — kolayca unit-testlenir.
public static class SandboxCreditScoreCalculator
{
    public const int BaseScore = 1000;
    public const int ClosedLoanBonus = 150;
    public const int ActiveLoanPenalty = 50;
    public const int HasOverduePenalty = 300;
    public const int OnTimePaymentBonus = 25;
    public const int OnTimePaymentBonusCap = 500;
    public const int OverdueInstallmentPenalty = 10;
    public const int NoHistoryPenalty = 100;
    public const int MinScore = 300;
    public const int MaxScore = 1900;

    public static ScoreBreakdown Calculate(IReadOnlyList<Loan> loans, DateTime nowUtc)
    {
        var factors = new List<CreditScoreFactor>
        {
            new CreditScoreFactor("base", "Başlangıç skoru", BaseScore),
        };

        int score = BaseScore;

        int closedLoans = loans.Count(l => l.Status == LoanStatus.Closed);
        if (closedLoans > 0)
        {
            int delta = closedLoans * ClosedLoanBonus;
            score += delta;
            factors.Add(new CreditScoreFactor(
                Code: "closed_loan_bonus",
                Label: $"{closedLoans} kapatılmış kredi",
                Delta: delta));
        }

        int activeLoans = loans.Count(l => l.Status == LoanStatus.Active);
        if (activeLoans > 0)
        {
            int delta = -activeLoans * ActiveLoanPenalty;
            score += delta;
            factors.Add(new CreditScoreFactor(
                Code: "active_loan_penalty",
                Label: $"{activeLoans} aktif kredi",
                Delta: delta));
        }

        var installments = loans
            .SelectMany(l => l.Installments ?? Enumerable.Empty<Installment>())
            .ToList();

        int paidInstallments = installments.Count(i => i.Status == InstallmentStatus.Paid);
        if (paidInstallments > 0)
        {
            int delta = Math.Min(paidInstallments * OnTimePaymentBonus, OnTimePaymentBonusCap);
            score += delta;
            factors.Add(new CreditScoreFactor(
                Code: "on_time_payment_bonus",
                Label: $"{paidInstallments} ödenmiş taksit",
                Delta: delta));
        }

        int overdueInstallments = installments.Count(i =>
            i.Status != InstallmentStatus.Paid && i.DueDate < nowUtc);

        if (overdueInstallments > 0)
        {
            int compounding = -overdueInstallments * OverdueInstallmentPenalty;
            score += compounding;
            factors.Add(new CreditScoreFactor(
                Code: "overdue_installment_penalty",
                Label: $"{overdueInstallments} gecikmiş taksit",
                Delta: compounding));

            score -= HasOverduePenalty;
            factors.Add(new CreditScoreFactor(
                Code: "has_overdue_penalty",
                Label: "Vadesi geçmiş borç riski",
                Delta: -HasOverduePenalty));
        }

        if (loans.Count == 0)
        {
            score -= NoHistoryPenalty;
            factors.Add(new CreditScoreFactor(
                Code: "no_history_penalty",
                Label: "Kredi geçmişi yok",
                Delta: -NoHistoryPenalty));
        }

        // Clamp
        int clamped = Math.Clamp(score, MinScore, MaxScore);
        if (clamped != score)
        {
            int clampDelta = clamped - score;
            factors.Add(new CreditScoreFactor(
                Code: "clamp",
                Label: clamped == MaxScore ? "Üst limit (1900)" : "Alt limit (300)",
                Delta: clampDelta));
        }

        var risk = ClassifyRisk(clamped);
        return new ScoreBreakdown(clamped, risk, factors);
    }

    public static string ClassifyRisk(int score) => score switch
    {
        >= 1500 => "VeryLow",
        >= 1100 => "Low",
        >= 800 => "Medium",
        >= 600 => "High",
        _ => "VeryHigh",
    };
}

public sealed record ScoreBreakdown(int Score, string RiskLevel, IReadOnlyList<CreditScoreFactor> Factors);
