using Microsoft.EntityFrameworkCore;
using LoanManagement.Business.Abstract;
using LoanManagement.DataAccess.Context;
using LoanManagement.Entities.DTOs;
using LoanManagement.Entities.Enums;
using LoanManagement.Entities.Models;

namespace LoanManagement.Business.Services;

public class CustomerService : ICustomerService
{
    private readonly LoanDbContext _context;

    public CustomerService(LoanDbContext context)
    {
        _context = context;
    }

    public async Task<List<CustomerListDto>> GetAllAsync()
    {
        var customers = await _context.Customers
            .Include(c => c.Loans)!
                .ThenInclude(l => l.Installments)
            .ToListAsync();

        return customers.Select(c => new CustomerListDto
        {
            Id = c.Id,
            FirstName = c.FirstName,
            LastName = c.LastName,
            Email = c.Email,
            PhoneNumber = c.PhoneNumber,
            TotalLoans = c.Loans?.Count ?? 0,
            Loans = (c.Loans ?? new List<Loan>()).Select(l => new LoanSummaryDto
            {
                LoanId = l.Id,
                LoanTypeName = GetLoanTypeName(l.LoanType),
                Status = l.Status.ToString(),
                Amount = l.Amount,
                Tenor = l.Tenor,
                StartDate = l.StartDate,
                TotalInstallments = l.Installments?.Count ?? 0,
                PaidInstallments = l.Installments?.Count(i => i.Status == InstallmentStatus.Paid) ?? 0,
                UnpaidInstallments = l.Installments?.Count(i => i.Status == InstallmentStatus.Unpaid) ?? 0,
                OverdueInstallments = l.Installments?.Count(i => i.Status == InstallmentStatus.Overdue) ?? 0,
                RemainingDebt = l.Installments?.Where(i => i.Status != InstallmentStatus.Paid).Sum(i => i.Amount) ?? 0
            }).ToList()
        }).ToList();
    }

    public async Task<CustomerResponseDto?> GetByIdAsync(int id)
    {
        var c = await _context.Customers
            .Include(c => c.Loans)!
                .ThenInclude(l => l.Installments)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (c == null) return null;

        return new CustomerResponseDto
        {
            Id = c.Id,
            FirstName = c.FirstName,
            LastName = c.LastName,
            Email = c.Email,
            PhoneNumber = c.PhoneNumber,
            IdentityNumber = c.IdentityNumber,
            TotalLoans = c.Loans?.Count ?? 0,
            Loans = (c.Loans ?? new List<Loan>()).Select(l => new LoanSummaryDto
            {
                LoanId = l.Id,
                LoanTypeName = GetLoanTypeName(l.LoanType),
                Status = l.Status.ToString(),
                Amount = l.Amount,
                Tenor = l.Tenor,
                StartDate = l.StartDate,
                TotalInstallments = l.Installments?.Count ?? 0,
                PaidInstallments = l.Installments?.Count(i => i.Status == InstallmentStatus.Paid) ?? 0,
                UnpaidInstallments = l.Installments?.Count(i => i.Status == InstallmentStatus.Unpaid) ?? 0,
                OverdueInstallments = l.Installments?.Count(i => i.Status == InstallmentStatus.Overdue) ?? 0,
                RemainingDebt = l.Installments?.Where(i => i.Status != InstallmentStatus.Paid).Sum(i => i.Amount) ?? 0
            }).ToList()
        };
    }

    public async Task<int> CreateAsync(CreateCustomerDto dto)
    {
        var exists = await _context.Customers.AnyAsync(c => c.IdentityNumber == dto.IdentityNumber);
        if (exists)
            throw new InvalidOperationException("Bu TC kimlik numarası ile kayıtlı müşteri zaten mevcut.");

        var customer = new Customer
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            IdentityNumber = dto.IdentityNumber,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber ?? ""
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer.Id;
    }

    public async Task<bool> UpdateAsync(int id, UpdateCustomerDto dto)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return false;

        customer.Email = dto.Email;
        customer.PhoneNumber = dto.PhoneNumber ?? customer.PhoneNumber;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var customer = await _context.Customers
            .Include(c => c.Loans)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer == null) return false;

        if (customer.Loans != null && customer.Loans.Any(l => l.Status == LoanStatus.Active))
            throw new InvalidOperationException("Aktif kredisi olan müşteri silinemez.");

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<CustomerSummaryDto?> GetCustomerSummaryAsync(int customerId)
    {
        var customer = await _context.Customers
            .Include(c => c.Loans)!
                .ThenInclude(l => l.Installments)
            .FirstOrDefaultAsync(c => c.Id == customerId);

        if (customer == null) return null;

        var allInstallments = customer.Loans!.SelectMany(l => l.Installments!).ToList();

        var loanDetails = customer.Loans!.Select(l => new LoanDetailSummaryDto
        {
            LoanId = l.Id,
            LoanTypeName = GetLoanTypeName(l.LoanType),
            Status = l.Status.ToString(),
            Amount = l.Amount,
            TotalPayable = l.Installments!.Sum(i => i.Amount),
            TotalPaid = l.Installments!.Where(i => i.Status == InstallmentStatus.Paid).Sum(i => i.Amount),
            RemainingDebt = l.Installments!.Where(i => i.Status != InstallmentStatus.Paid).Sum(i => i.Amount),
            TotalInstallments = l.Installments!.Count,
            PaidInstallments = l.Installments!.Count(i => i.Status == InstallmentStatus.Paid),
            UnpaidInstallments = l.Installments!.Count(i => i.Status == InstallmentStatus.Unpaid),
            OverdueInstallments = l.Installments!.Count(i => i.Status == InstallmentStatus.Overdue)
        }).ToList();

        return new CustomerSummaryDto
        {
            CustomerId = customer.Id,
            FullName = $"{customer.FirstName} {customer.LastName}",
            TotalLoans = customer.Loans!.Count,
            ActiveLoans = customer.Loans!.Count(l => l.Status == LoanStatus.Active),
            ClosedLoans = customer.Loans!.Count(l => l.Status == LoanStatus.Closed),
            TotalDebt = allInstallments.Where(i => i.Status != InstallmentStatus.Paid).Sum(i => i.Amount),
            TotalPaid = allInstallments.Where(i => i.Status == InstallmentStatus.Paid).Sum(i => i.Amount),
            TotalInstallments = allInstallments.Count,
            PaidInstallments = allInstallments.Count(i => i.Status == InstallmentStatus.Paid),
            UnpaidInstallments = allInstallments.Count(i => i.Status == InstallmentStatus.Unpaid),
            OverdueInstallments = allInstallments.Count(i => i.Status == InstallmentStatus.Overdue),
            Loans = loanDetails
        };
    }

    private static string GetLoanTypeName(LoanType loanType) => loanType switch
    {
        LoanType.Personal => "İhtiyaç Kredisi",
        LoanType.Education => "Eğitim Kredisi",
        LoanType.Vehicle => "Araç Kredisi",
        _ => "Bilinmeyen Kredi Türü"
    };
}
