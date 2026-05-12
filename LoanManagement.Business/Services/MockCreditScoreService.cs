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
        // Müşterinin olup olmadığını kontrol et
        var customerExists = await _context.Customers.AnyAsync(c => c.Id == customerId);
        if (!customerExists)
            throw new KeyNotFoundException($"Customer with id {customerId} not found.");

        await Task.Delay(300);

        var loans = await _context.Loans
            .Include(l => l.Installments)
            .Where(l => l.CustomerId == customerId)
            .ToListAsync();

        int score = 1000; // istediğim başlangıç
        int closedLoansCount = loans.Count(l => l.Status == LoanStatus.Closed);
        score += (closedLoansCount * 150);
        int activeLoansCount = loans.Count(l => l.Status == LoanStatus.Active);
        score -= (activeLoansCount * 50);

        bool hasLatePayment = loans
            .SelectMany(l => l.Installments)
            .Any(i => i.DueDate < DateTime.Now && i.Status == InstallmentStatus.Unpaid);

        if (hasLatePayment) score -= 300;

        // Eğer müşteri hiç kredi yoksa
        if (loans.Count == 0)
            score -= 100;

        if (score < 300) score = 300;
        if (score > 1900) score = 1900;

        return score;
    }
}