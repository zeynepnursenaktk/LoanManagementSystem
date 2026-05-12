using Microsoft.EntityFrameworkCore;
using LoanManagement.Business.Abstract;
using LoanManagement.DataAccess.Context;
using LoanManagement.Entities.DTOs;
using LoanManagement.Entities.Enums;
using LoanManagement.Entities.Models;
using LoanManagement.Entities.Validation;

namespace LoanManagement.Business.Services;

public class CustomerService : ICustomerService
{
    private readonly LoanDbContext _context;

    public CustomerService(LoanDbContext context)
    {
        _context = context;
    }

    //Tüm müşterileri getir.
    public async Task<List<CustomerListDto>> GetCustomersAsync(bool includeDeleted = false)
    {
        // includeDeleted = true → global soft-delete query filter'ı bypass edilir.
        // Loan navigation'ında da Customer.IsDeleted filtresi var; aynı şekilde bypass.
        IQueryable<Customer> query = _context.Customers
            .Include(c => c.Loans)!
                .ThenInclude(l => l.Installments);

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var customers = await query.ToListAsync();

        return customers.Select(c => new CustomerListDto
        {
            Id = c.Id,
            FirstName = c.FirstName,
            LastName = c.LastName,
            Email = c.Email,
            PhoneNumber = c.PhoneNumber,
            IsDeleted = c.IsDeleted,
            DeletedAtUtc = c.DeletedAtUtc,
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


    //Belirli bir müşteriyi ID'sine göre getir.              
    public async Task<CustomerResponseDto?> GetCustomerByIdAsync(int customerId)
    {
        var c = await _context.Customers
            .Include(c => c.Loans)!
                .ThenInclude(l => l.Installments)
            .FirstOrDefaultAsync(c => c.Id == customerId);

        if (c == null) return null;

        return new CustomerResponseDto
        {
            Id = c.Id,
            FirstName = c.FirstName,
            LastName = c.LastName,
            Email = c.Email,
            PhoneNumber = c.PhoneNumber,
            IdentityNumber = c.IdentityNumber,
            IsDeleted = c.IsDeleted,
            DeletedAtUtc = c.DeletedAtUtc,
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


    //Müşteri oluşturma
    public async Task<int> CreateCustomerAsync(CreateCustomerDto dto)
    {
        // Defensive validation (ham değerlerle)
        if (!TurkishIdentityNumberValidator.IsValid(dto.IdentityNumber))
            throw new ArgumentException("Geçersiz T.C. Kimlik Numarası.", nameof(dto.IdentityNumber));

        if (!TurkishPhoneNumberValidator.IsValid(dto.PhoneNumber))
            throw new ArgumentException("Geçersiz telefon numarası formatı.", nameof(dto.PhoneNumber));

        // Normalize girişler (depolama için temizlenmiş hâl)
        var firstName = (dto.FirstName ?? string.Empty).Trim();
        var lastName = (dto.LastName ?? string.Empty).Trim();
        var identityNumber = (dto.IdentityNumber ?? string.Empty).Trim();
        var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();
        var phoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber)
            ? null
            : TurkishPhoneNumberValidator.Normalize(dto.PhoneNumber!);

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName)
            || string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Zorunlu alanlar boş bırakılamaz.");

        //Gerekli benzersizlik kontrolleri
        var identityExists = await _context.Customers.AnyAsync(c => c.IdentityNumber == identityNumber);
        if (identityExists)
            throw new InvalidOperationException("Bu TC kimlik numarası ile kayıtlı müşteri zaten mevcut.");

        var emailExists = await _context.Customers.AnyAsync(c => c.Email == email);
        if (emailExists)
            throw new InvalidOperationException("Bu e-posta adresi zaten başka bir müşteri tarafından kullanılmaktadır.");

        var customer = new Customer
        {
            FirstName = firstName,
            LastName = lastName,
            IdentityNumber = identityNumber,
            Email = email,
            PhoneNumber = phoneNumber ?? ""
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return customer.Id;
    }

    //Müşteri güncelleme 
    public async Task<bool> UpdateCustomerAsync(int customerId, UpdateCustomerDto dto)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null) return false;

        // Defensive validation (ham değerlerle)
        if (!TurkishPhoneNumberValidator.IsValid(dto.PhoneNumber))
            throw new ArgumentException("Geçersiz telefon numarası formatı.", nameof(dto.PhoneNumber));

        // Normalize
        var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();
        var phoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber)
            ? null
            : TurkishPhoneNumberValidator.Normalize(dto.PhoneNumber!);

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("E-posta adresi boş bırakılamaz.", nameof(dto.Email));

        //başka bir müşteri zaten bu email'i kullanıyor mu? Kontrol
        var emailExists = await _context.Customers
            .AnyAsync(c => c.Email == email && c.Id != customerId);

        if (emailExists)
            throw new InvalidOperationException("Bu e-posta adresi zaten başka bir müşteri tarafından kullanılmaktadır.");

        customer.Email = email;
        customer.PhoneNumber = phoneNumber ?? customer.PhoneNumber;
        await _context.SaveChangesAsync();
        return true;
    }


    //Soft Delete
    public async Task<bool> DeleteCustomerAsync(int customerId)
    {
        var customer = await _context.Customers
            .Include(c => c.Loans)
            .FirstOrDefaultAsync(c => c.Id == customerId);

        if (customer == null) return false;

        if (customer.Loans != null && customer.Loans.Any(l => l.Status == LoanStatus.Active))
            throw new InvalidOperationException("Aktif kredisi olan müşteri silinemez.");

        customer.IsDeleted = true;
        customer.DeletedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    /// Soft-delete edilmiş müşteriyi aktife çevirir. Global query filter bypass
    /// edildiği için soft-delete edilmiş kayıtlar erişilebilir hale gelir.
    public async Task<bool> RestoreCustomerAsync(int customerId)
    {
        var customer = await _context.Customers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == customerId);

        if (customer == null) return false;

        // Zaten aktifse no-op; çağıran taraf 200 yerine 409/304 dönmek isterse
        // bool sonucu üzerinden ayırt edebilir.
        if (!customer.IsDeleted) return false;

        customer.IsDeleted = false;
        customer.DeletedAtUtc = null;
        await _context.SaveChangesAsync();
        return true;
    }

    //Müşteri özet bilgisi 
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
