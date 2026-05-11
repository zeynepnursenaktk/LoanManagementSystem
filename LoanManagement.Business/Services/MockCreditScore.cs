using LoanManagement.Business.Abstract;
using LoanManagement.DataAccess.Context;
using LoanManagement.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace LoanManagement.Business.Services;

public class MockCreditScoreService : ICreditScoreService
{
    private readonly LoanDbContext _context;

    public MockCreditScoreService(LoanDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetCreditScoreAsync(int customerId)
    {
        // Gerçek bir servis çağrısı yapıyormuş gibi bekleme süresi 
        await Task.Delay(300);

        // Müşterinin tüm kredi geçmişini taksitleriyle birlikte çekelim
        var loans = await _context.Loans
            .Include(l => l.Installments)
            .Where(l => l.CustomerId == customerId)
            .ToListAsync();

        // Herkes 1000 puan ile başlar 
        int score = 1000;

        // Kural 1: Daha önce başarıyla kapatılmış her kredi için +150 puan
        int closedLoansCount = loans.Count(l => l.Status == LoanStatus.Closed);
        score += (closedLoansCount * 150);

        // Kural 2: Mevcut her aktif kredi bir yüktür, -50 puan
        int activeLoansCount = loans.Count(l => l.Status == LoanStatus.Active);
        score -= (activeLoansCount * 50);

        // Kural 3: Geçmişte (veya şu an) ödenmemiş ve vadesi geçmiş taksit varsa büyük ceza, -300 puan
        bool hasLatePayment = loans
            .SelectMany(l => l.Installments)
            .Any(i => i.DueDate < DateTime.Now && i.Status == InstallmentStatus.Unpaid);

        if (hasLatePayment)
        {
            score -= 300;
        }

        // Kural 4: Müşterinin hiç kredisi yoksa (Yeni müşteri), güven tazelemek için başlangıç puanı düşer, -100 puan
        if (loans.Count == 0)
        {
            score -= 100;
        }

        // Skor Sınırları: Bankacılık standartlarına göre 300 ile 1900 arasında tutalım
        if (score < 300) score = 300;
        if (score > 1900) score = 1900;

        return score;
    }
}