using Microsoft.EntityFrameworkCore;
using LoanManagement.Business.Abstract;
using LoanManagement.DataAccess.Context;
using LoanManagement.Entities.Enums;
using LoanManagement.Entities.DTOs;

namespace LoanManagement.Business.Services;

public class InstallmentService : IInstallmentService
{
    private readonly LoanDbContext _context;

    public InstallmentService(LoanDbContext context)
    {
        _context = context;
    }


    //Krediye ait taksitler
    public async Task<List<InstallmentDto>> GetByLoanIdAsync(int loanId)
    {
        return await _context.Installments
            .Include(i => i.Payment)
            .Where(i => i.LoanId == loanId)
            .OrderBy(i => i.InstallmentNumber)
            .Select(i => MapToDto(i))
            .ToListAsync();
    }

    //Tek taksit bilgisi 
    public async Task<InstallmentDto?> GetByIdAsync(int id)
    {
        var installment = await _context.Installments
            .Include(i => i.Payment)
            .FirstOrDefaultAsync(i => i.Id == id);

        return installment == null ? null : MapToDto(installment);
    }

    //Ödenmemiş taksitler 
    public async Task<List<InstallmentDto>> GetUnpaidByCustomerIdAsync(int customerId)
    {
        return await _context.Installments
            .Include(i => i.Loan)
            .Include(i => i.Payment)
            .Where(i => i.Loan!.CustomerId == customerId && i.Status != InstallmentStatus.Paid)
            .OrderBy(i => i.DueDate)
            .Select(i => MapToDto(i))
            .ToListAsync();
    }

    //Vadesi geçmiş ödenmemiş taksitler
    public async Task<List<InstallmentDto>> GetOverdueByCustomerIdAsync(int customerId)
    {
        return await _context.Installments
            .Include(i => i.Loan)
            .Include(i => i.Payment)
            .Where(i => i.Loan!.CustomerId == customerId && i.Status == InstallmentStatus.Overdue)
            .OrderBy(i => i.DueDate)
            .Select(i => MapToDto(i))
            .ToListAsync();
    }

    /// Vadesi geçmiş ödenmemiş taksitleri "Overdue" olarak günceller.
    public async Task<int> UpdateOverdueInstallmentsAsync()
    {
        var now = DateTime.UtcNow;
        var overdueInstallments = await _context.Installments
            .Where(i => i.Status == InstallmentStatus.Unpaid && i.DueDate < now)
            .ToListAsync();

        foreach (var installment in overdueInstallments)
        {
            installment.Status = InstallmentStatus.Overdue;
        }

        await _context.SaveChangesAsync();
        return overdueInstallments.Count;
    }


    //Yetki kontrolü için yardımcı metotlar
    //Controllerda kullanıyorum: başkasının taksitiyle işlem yapmamak için.
    public Task<bool> LoanBelongsToCustomerAsync(int loanId, int customerId) =>
        _context.Loans.AnyAsync(l => l.Id == loanId && l.CustomerId == customerId);

    public Task<bool> InstallmentBelongsToCustomerAsync(int installmentId, int customerId) =>
        _context.Installments.AnyAsync(i => i.Id == installmentId && i.Loan != null && i.Loan.CustomerId == customerId);

    private static InstallmentDto MapToDto(Entities.Models.Installment i)
    {
        return new InstallmentDto
        {
            Id = i.Id,
            LoanId = i.LoanId,
            InstallmentNumber = i.InstallmentNumber,
            Amount = i.Amount,
            DueDate = i.DueDate,
            Status = i.Status.ToString(),
            IsPaid = i.Status == InstallmentStatus.Paid,
            PaidAmount = i.Payment?.Amount,
            PaymentDate = i.Payment?.PaymentDate
        };
    }
}
