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

    // Belirli bir krediye ait tüm ödeme kayıtlarını getirir.
    // Taksitler taksit numarasına göre sıralı döner.
    public async Task<List<Installment>> GetInstallmentsByLoanIdAsync(int loanId)
    {
        return await _context.Installments
            .Where(i => i.LoanId == loanId)
            .OrderBy(i => i.InstallmentNumber)
            .ToListAsync();
    }

    // Sisteme kaydedilmiş tüm ödeme geçmişini döner.
    public async Task<List<Payment>> GetAllPaymentsAsync()
    {
        return await _context.Payments
            .Include(p => p.Installment)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();
    }
}