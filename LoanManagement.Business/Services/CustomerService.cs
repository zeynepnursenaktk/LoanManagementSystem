using Microsoft.EntityFrameworkCore;
using LoanManagement.Business.Abstract;
using LoanManagement.DataAccess.Context;
using LoanManagement.Entities.DTOs;
using LoanManagement.Entities.Enums;
using LoanManagement.Entities.Models;

public class CustomerService : ICustomerService
{
    private readonly LoanDbContext _context;

    public CustomerService(LoanDbContext context)
    {
        _context = context;
    }

    // Tüm müşteriler: opsiyonel olarak kredilerin kısa özetlerini de getirir.
    public async Task<List<CustomerListDto>> GetAllAsync()
    {
        var query = _context.Customers.AsQueryable();

        return await query
            .Select(c => new CustomerListDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                TotalLoans = c.Loans.Count()    // DB tarafında hesaplanır
            })
            .ToListAsync();


    }

    // Verilen id'ye sahip müşterinin detaylarını döner. Kredileri kısa özetleriyle birlikte getirir.
    public async Task<CustomerResponseDto?> GetByIdAsync(int id)
    {
        return await _context.Customers
            .Where(c => c.Id == id)
            .Select(c => new CustomerResponseDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                IdentityNumber = c.IdentityNumber,
                Loans = c.Loans.Select(l => new LoanSummaryDto
                {
                    LoanId = l.Id,
                    LoanTypeName = GetLoanTypeName(l.LoanType),
                    Status = l.Status.ToString(),
                    Amount = l.Amount,
                    Tenor = l.Tenor,
                    StartDate = l.StartDate,
                    TotalInstallments = l.Installments.Count()   // DB tarafında hesaplanır
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    // Yeni müşteri oluşturma (DTO alır), ID döner.
    public async Task<int> CreateAsync(CreateCustomerDto dto)
    {
        var customer = new Customer
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            IdentityNumber = dto.IdentityNumber,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer.Id;
    }

    // Sadece email ve telefon güncellemesi
    public async Task<bool> UpdateAsync(int id, UpdateCustomerDto dto)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return false;

        customer.Email = dto.Email;
        customer.PhoneNumber = dto.PhoneNumber;
        await _context.SaveChangesAsync();
        return true;
    }

    // Müşterinin kredi/taksit özetini döner.
    public async Task<CustomerSummaryDto?> GetCustomerSummaryAsync(int customerId)
    {
        var customer = await _context.Customers
            .Include(c => c.Loans)
                .ThenInclude(l => l.Installments)
            .FirstOrDefaultAsync(c => c.Id == customerId);

        if (customer == null) return null;

        var allInstallments = customer.Loans.SelectMany(l => l.Installments).ToList();

        var loanDetails = customer.Loans.Select(l => new LoanDetailSummaryDto
        {
            LoanId = l.Id,
            LoanTypeName = GetLoanTypeName(l.LoanType),
            Status = l.Status.ToString(),
            Amount = l.Amount,
            TotalPayable = l.Installments.Sum(i => i.Amount),
            TotalPaid = l.Installments.Where(i => i.Status == InstallmentStatus.Paid).Sum(i => i.Amount),
            RemainingDebt = l.Installments.Where(i => i.Status == InstallmentStatus.Unpaid).Sum(i => i.Amount),
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
            TotalDebt = allInstallments.Where(i => i.Status == InstallmentStatus.Unpaid).Sum(i => i.Amount),
            TotalPaid = allInstallments.Where(i => i.Status == InstallmentStatus.Paid).Sum(i => i.Amount),
            TotalInstallments = allInstallments.Count,
            PaidInstallments = allInstallments.Count(i => i.Status == InstallmentStatus.Paid),
            UnpaidInstallments = allInstallments.Count(i => i.Status == InstallmentStatus.Unpaid),
            Loans = loanDetails
        };
    }

    // Tekrar eden switch ifadelerini tek noktada toplamak için private yardımcı metod.
    private static string GetLoanTypeName(LoanType loanType) => loanType switch
    {
        LoanType.Personal => "İhtiyaç Kredisi",
        LoanType.Education => "Eğitim Kredisi",
        LoanType.Vehicle => "Araç Kredisi",
        _ => "Bilinmeyen Kredi Türü"
    };
}
