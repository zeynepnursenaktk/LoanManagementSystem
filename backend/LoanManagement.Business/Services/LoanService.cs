using Microsoft.EntityFrameworkCore;
using LoanManagement.Business.Abstract;
using LoanManagement.DataAccess.Context;
using LoanManagement.Entities.Models;
using LoanManagement.Entities.Enums;
using LoanManagement.Entities.DTOs;

namespace LoanManagement.Business.Services;

public class LoanService : ILoanService
{
    private readonly LoanDbContext _context;
    private readonly ICreditScoreService _creditScoreService;

    public LoanService(LoanDbContext context, ICreditScoreService creditScoreService)
    {
        _context = context;
        _creditScoreService = creditScoreService;
    }

    /// Yeni kredi oluşturur ve taksit planını otomatik olarak hesaplar.
    /// Kar oranı yıllık yüzde (0–100) olarak gelir; hesaplamada çarpana (÷100) dönüştürülür.
    /// Formül: ToplamGeriÖdeme = AnaPara + (AnaPara × (Yüzde/100) × VadeAy / 12)
    public async Task<int> CreateLoanAsync(LoanRequestDto loanDto)
    {
        // Müşteri var mı kontrol et
        var customerExists = await _context.Customers.AnyAsync(c => c.Id == loanDto.CustomerId);
        if (!customerExists)
            throw new KeyNotFoundException("Müşteri bulunamadı.");

        // Kredi skoru kontrolü (Mock servis)
        int creditScore = await _creditScoreService.GetCreditScoreAsync(loanDto.CustomerId);
        if (creditScore < 600)
            throw new InvalidOperationException($"Müşterinin kredi skoru ({creditScore}) yetersiz. Minimum 600 gereklidir.");

        // Validasyonlar
        if (loanDto.Amount <= 0)
            throw new ArgumentException("Kredi tutarı 0'dan büyük olmalıdır.");
        if (loanDto.Tenor <= 0 || loanDto.Tenor > 120)
            throw new ArgumentException("Vade 1-120 ay arasında olmalıdır.");
        // ProfitRate: API yüzde (0–100); veritabanında yıllık çarpan (örn. %24 → 0,24) saklanır.
        if (loanDto.ProfitRate < 0 || loanDto.ProfitRate > 100)
            throw new ArgumentException("Yıllık kar oranı 0 ile 100 arasında olmalıdır (yüzde).");

        decimal annualFactor = loanDto.ProfitRate / 100m;

        if (!Enum.IsDefined(typeof(LoanType), loanDto.LoanType))
            throw new ArgumentException("Geçersiz kredi türü. 0: İhtiyaç, 1: Eğitim, 2: Taşıt");

        var loan = new Loan
        {
            CustomerId = loanDto.CustomerId,
            Amount = loanDto.Amount,
            Tenor = loanDto.Tenor,
            ProfitRate = annualFactor,
            StartDate = loanDto.StartDate,
            LoanType = loanDto.LoanType,
            Status = LoanStatus.Active
        };

        // *** TAKSIT HESAPLAMA ***
        // loan.ProfitRate = yıllık çarpan (API'deki yüzde / 100).
        // Toplam kar = AnaPara × YıllıkÇarpan × (VadeAy / 12)
        decimal totalProfit = loan.Amount * loan.ProfitRate * ((decimal)loan.Tenor / 12m);
        decimal totalPayable = loan.Amount + totalProfit;
        decimal monthlyAmount = Math.Round(totalPayable / loan.Tenor, 2);

        // Son taksitte kuruş farkını düzelt
        decimal lastInstallmentAmount = totalPayable - (monthlyAmount * (loan.Tenor - 1));


        //her aya bir taksit
        for (int i = 1; i <= loan.Tenor; i++)
        {
            var installment = new Installment
            {
                InstallmentNumber = i,
                Amount = (i == loan.Tenor) ? lastInstallmentAmount : monthlyAmount,
                DueDate = loan.StartDate.AddMonths(i),
                Status = InstallmentStatus.Unpaid
            };
            loan.Installments!.Add(installment);
        }

        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();

        return loan.Id;
    }

    public async Task<LoanResponseDto?> GetLoanByIdAsync(int loanId)
    {
        var loan = await _context.Loans
            .Include(l => l.Installments)!.ThenInclude(i => i.Payment)
            .Include(l => l.Customer)
            .FirstOrDefaultAsync(l => l.Id == loanId);

        if (loan == null) return null;
        return MapToDto(loan);
    }

    public async Task<List<LoanResponseDto>> GetLoansAsync()
    {
        var loans = await _context.Loans
            .Include(l => l.Customer)
            .Include(l => l.Installments)!.ThenInclude(i => i.Payment)
            .OrderByDescending(l => l.Id)
            .ToListAsync();

        return loans.Select(MapToDto).ToList();
    }

    public async Task<List<LoanResponseDto>> GetLoansByCustomerIdAsync(int customerId)
    {
        var loans = await _context.Loans
            .Include(l => l.Customer)
            .Include(l => l.Installments)!.ThenInclude(i => i.Payment)
            .Where(l => l.CustomerId == customerId)
            .OrderByDescending(l => l.Id)
            .ToListAsync();

        return loans.Select(MapToDto).ToList();
    }

    private static LoanResponseDto MapToDto(Loan loan)
    {
        return new LoanResponseDto
        {
            Id = loan.Id,
            CustomerId = loan.Customer!.Id,
            CustomerFullName = $"{loan.Customer.FirstName} {loan.Customer.LastName}",
            LoanTypeName = GetLoanTypeName(loan.LoanType),
            Amount = loan.Amount,
            Tenor = loan.Tenor,
            ProfitRate = Math.Round(loan.ProfitRate * 100m, 4, MidpointRounding.AwayFromZero),
            TotalPayable = loan.Installments!.Sum(i => i.Amount),
            StartDate = loan.StartDate,
            Status = loan.Status.ToString(),
            Installments = loan.Installments!.OrderBy(i => i.InstallmentNumber).Select(i => new InstallmentDto
            {
                Id = i.Id,
                LoanId = loan.Id,
                InstallmentNumber = i.InstallmentNumber,
                Amount = i.Amount,
                DueDate = i.DueDate,
                Status = i.Status.ToString(),
                IsPaid = i.Status == InstallmentStatus.Paid,
                PaidAmount = i.Payment?.Amount,
                PaymentDate = i.Payment?.PaymentDate
            }).ToList()
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
