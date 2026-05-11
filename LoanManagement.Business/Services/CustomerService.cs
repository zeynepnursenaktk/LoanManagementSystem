using Microsoft.EntityFrameworkCore;
using LoanManagement.Business.Abstract;
using LoanManagement.DataAccess.Context;
using LoanManagement.Entities.Models;
using LoanManagement.Entities.DTOs;
using LoanManagement.Entities.Enums;


namespace LoanManagement.Business.Services;

public class CustomerService : ICustomerService
{
    private readonly LoanDbContext _context;

    public CustomerService(LoanDbContext context)
    {
        _context = context;
    }

    // Veritabanındaki tüm müşterileri liste olarak döner.
    // Krediler dahil edilmez, sadece müşteri bilgileri gelir.
    public async Task<List<Customer>> GetAllAsync()
    {
        return await _context.Customers
            .Include(c => c.Loans) // Kredileri de getir
            .ToListAsync();
    }

    // Belirtilen ID'ye sahip müşteriyi, kredileriyle birlikte döner.
    // Include ile Loans tablosu JOIN'lenir. Müşteri bulunamazsa null döner.
    public async Task<Customer?> GetByIdAsync(int id)
    {
        return await _context.Customers
            .Include(c => c.Loans) // Müşterinin kredilerini de getir
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    // Yeni bir müşteri kaydı oluşturur ve veritabanına kaydeder.
    public async Task AddAsync(Customer customer)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
    }

    public async Task<CustomerSummaryDto?> GetCustomerSummaryAsync(int customerId)
    {
        var customer = await _context.Customers
            .Include(c => c.Loans)
                .ThenInclude(l => l.Installments)
            .FirstOrDefaultAsync(c => c.Id == customerId);

        if (customer == null) return null;

        var allInstallments = customer.Loans
            .SelectMany(l => l.Installments)
            .ToList();

        // Her kredi için ayrı özet oluştur
        var loanDetails = customer.Loans.Select(l => new LoanDetailSummaryDto
        {
            LoanId = l.Id,
            LoanTypeName = l.LoanType switch
            {
                LoanType.Personal => "İhtiyaç Kredisi",
                LoanType.Education => "Eğitim Kredisi",
                LoanType.Vehicle => "Araç Kredisi",
                _ => "Bilinmeyen"
            },
            Status = l.Status.ToString(),
            Amount = l.Amount,
            TotalPayable = l.Installments.Sum(i => i.Amount),
            TotalPaid = l.Installments
                .Where(i => i.Status == InstallmentStatus.Paid)
                .Sum(i => i.Amount),
            RemainingDebt = l.Installments
                .Where(i => i.Status == InstallmentStatus.Unpaid)
                .Sum(i => i.Amount),
            TotalInstallments = l.Installments.Count,
            PaidInstallments = l.Installments.Count(i => i.Status == InstallmentStatus.Paid),
            UnpaidInstallments = l.Installments.Count(i => i.Status == InstallmentStatus.Unpaid)
        }).ToList();

        return new CustomerSummaryDto
        {
            CustomerId = customer.Id,
            FullName = $"{customer.FirstName} {customer.LastName}",
            TotalLoans = customer.Loans.Count,
            ActiveLoans = customer.Loans.Count(l => l.Status == LoanStatus.Active),
            ClosedLoans = customer.Loans.Count(l => l.Status == LoanStatus.Closed),
            TotalDebt = allInstallments
                .Where(i => i.Status == InstallmentStatus.Unpaid)
                .Sum(i => i.Amount),
            TotalPaid = allInstallments
                .Where(i => i.Status == InstallmentStatus.Paid)
                .Sum(i => i.Amount),
            TotalInstallments = allInstallments.Count,
            PaidInstallments = allInstallments.Count(i => i.Status == InstallmentStatus.Paid),
            UnpaidInstallments = allInstallments.Count(i => i.Status == InstallmentStatus.Unpaid),
            Loans = loanDetails
        };
    }

    // Müşterinin sadece email ve telefon bilgisini günceller.
    // TC kimlik, isim gibi kritik alanlar bu metod üzerinden değiştirilemez.
    // Müşteri bulunamazsa false, başarılıysa true döner.
    public async Task<bool> UpdateAsync(int id, UpdateCustomerDto dto)
    {
        var customer = await _context.Customers.FindAsync(id);

        if (customer == null)
            return false;

        customer.Email = dto.Email;
        customer.PhoneNumber = dto.PhoneNumber;

        await _context.SaveChangesAsync();
        return true;
    }

}