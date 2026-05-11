using Microsoft.EntityFrameworkCore;
using LoanManagement.Business.Abstract;
using LoanManagement.DataAccess.Context;
using LoanManagement.Entities.Models;
using LoanManagement.Entities.Enums;

namespace LoanManagement.Business.Services;

public class LoanService : ILoanService
{
    private readonly LoanDbContext _context;

    public LoanService(LoanDbContext context)
    {
        _context = context;
    }

    public async Task CreateLoanWithInstallmentsAsync(Loan loan)
    {
        // 1. Toplam maliyeti hesapla (Ana Para + Kar)
        // Not: Basit usul Kar Oranı hesaplaması örneği
        decimal totalPayable = loan.Amount + (loan.Amount * loan.ProfitRate);
        
        // 2. Aylık taksit tutarını bul
        decimal monthlyAmount = totalPayable / loan.Tenor;

        loan.Status = LoanStatus.Active; // Kredi aktifleşti
        
        // 3. Taksitleri döngüyle oluştur
        for (int i = 1; i <= loan.Tenor; i++)
        {
            var installment = new Installment
            {
                InstallmentNumber = i,
                Amount = monthlyAmount,
                // Her taksit bir önceki taksitten 30 gün sonra
                DueDate = loan.StartDate.AddMonths(i), 
                Status = InstallmentStatus.Unpaid // Henüz ödenmedi
            };
            
            // Taksiti kredi nesnesinin içine ekle (EF Core aradaki bağı otomatik kuracaktır)
            loan.Installments.Add(installment);
        }

        // 4. Veritabanına kaydet
        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Loan>> GetAllLoansAsync()
    {
        // Kredileri çekerken içindeki taksitleri de "Include" (dahil et) diyoruz ki boş gelmesinler
        return await _context.Loans.Include(l => l.Installments).ToListAsync();
    }

    public async Task<Loan?> GetLoanByIdAsync(int id)
    {
        return await _context.Loans
            .Include(l => l.Installments)
            .Include(l => l.Customer)
            .FirstOrDefaultAsync(l => l.Id == id);
    }
}