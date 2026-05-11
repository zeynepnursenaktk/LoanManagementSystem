using Microsoft.EntityFrameworkCore;
using LoanManagement.Business.Abstract;
using LoanManagement.DataAccess.Context;
using LoanManagement.Entities.Models;
using LoanManagement.Entities.Enums;

namespace LoanManagement.Business.Services;

public class PaymentService : IPaymentService
{
    private readonly LoanDbContext _context;

    public PaymentService(LoanDbContext context)
    {
        _context = context;
    }

    public async Task PayInstallmentAsync(int installmentId, decimal amount)
    {
        // 1. Taksiti bul
        var installment = await _context.Installments
            .Include(i => i.Loan) // Krediyi de getir (kapanma kontrolü için)
            .FirstOrDefaultAsync(i => i.Id == installmentId);

        if (installment == null)
            throw new Exception("Taksit bulunamadı.");

        if (installment.Status == InstallmentStatus.Paid)
            throw new Exception("Bu taksit zaten ödenmiş.");

        // 2. Ödemeyi kaydet
        var payment = new Payment
        {
            InstallmentId = installmentId,
            Amount = amount,
            PaymentDate = DateTime.Now
        };
        _context.Payments.Add(payment);

        // 3. Taksit durumunu güncelle
        installment.Status = InstallmentStatus.Paid;

        // 4. Kredi kapanma kontrolü
        // Bu kredinin tüm taksitleri ödendi mi?
        var allInstallments = await _context.Installments
            .Where(i => i.LoanId == installment.LoanId)
            .ToListAsync();

        bool allPaid = allInstallments.All(i => 
            i.Id == installmentId || i.Status == InstallmentStatus.Paid);

        if (allPaid)
            installment.Loan.Status = LoanStatus.Closed; // Krediyi kapat

        await _context.SaveChangesAsync();
    }

    public async Task<List<Installment>> GetInstallmentsByLoanIdAsync(int loanId)
    {
        return await _context.Installments
            .Where(i => i.LoanId == loanId)
            .OrderBy(i => i.InstallmentNumber) // Sıralı gelsin
            .ToListAsync();
    }
}