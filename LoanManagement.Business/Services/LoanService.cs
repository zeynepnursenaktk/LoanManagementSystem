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

    public LoanService(LoanDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateLoanWithInstallmentsAsync(LoanRequestDto loanDto)
    {
        if (loanDto.Tenor <= 0)
            throw new ArgumentException("Vade (Tenor) değeri 0 olamaz!", nameof(loanDto.Tenor));

        if (loanDto.Amount <= 0)
            throw new ArgumentException("Kredi tutarı (Amount) 0 olamaz!", nameof(loanDto.Amount));

        if (!Enum.IsDefined(typeof(LoanType), loanDto.LoanType))
            throw new ArgumentException("Geçersiz kredi türü!", nameof(loanDto.LoanType));

        var loan = new Loan
        {
            CustomerId = loanDto.CustomerId,
            Amount = loanDto.Amount,
            Tenor = loanDto.Tenor,
            ProfitRate = loanDto.ProfitRate,
            StartDate = loanDto.StartDate,
            LoanType = loanDto.LoanType,
            Status = LoanStatus.Active
        };

        decimal totalPayable = loan.Amount + (loan.Amount * loan.ProfitRate);
        decimal monthlyAmount = totalPayable / loan.Tenor;

        for (int i = 1; i <= loan.Tenor; i++)
        {
            var installment = new Installment
            {
                InstallmentNumber = i,
                Amount = monthlyAmount,
                DueDate = loan.StartDate.AddMonths(i),
                Status = InstallmentStatus.Unpaid
            };

            loan.Installments.Add(installment);
        }

        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();

        return loan.Id; 
    }


    // ID'ye göre tek bir krediyi taksitleri ve müşteri bilgisiyle birlikte getirir.
    public async Task<LoanResponseDto?> GetLoanByIdDtoAsync(int id)
    {

        var loan = await _context.Loans
            .Include(l => l.Installments)
            .Include(l => l.Customer)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (loan == null) return null;

        if (!Enum.IsDefined(typeof(LoanType), loan.LoanType))
            throw new Exception("Geçersiz kredi türü! 0: İhtiyaç, 1: Eğitim, 2: Araç");

        var response = new LoanResponseDto
        {
            Id = loan.Id,
            CustomerId = loan.Customer.Id,
            CustomerFullName = $"{loan.Customer.FirstName} {loan.Customer.LastName}",
            LoanTypeName = GetLoanTypeName(loan.LoanType),
            Amount = loan.Amount,
            Tenor = loan.Tenor,
            ProfitRate = loan.ProfitRate,
            StartDate = loan.StartDate,
            Status = loan.Status.ToString(),

            Installments = loan.Installments.Select(i => new InstallmentDto
            {
                Id = i.Id,
                InstallmentNumber = i.InstallmentNumber,
                Amount = i.Amount,
                DueDate = i.DueDate,
                Status = i.Status.ToString()
            }).ToList()
        };

        return response;
    }

    public async Task<PaymentResponseDto?> PayInstallmentAsync(PaymentRequestDto request)
    {
        // Müşterinin tüm kredilerini çek, sıralı şekilde
        var loans = await _context.Loans
            .Include(l => l.Customer)
            .Include(l => l.Installments)
            .Where(l => l.CustomerId == request.CustomerId)
            .OrderBy(l => l.Id)
            .ToListAsync();

        if (loans.Count == 0)
            throw new Exception("Bu müşteriye ait kredi bulunamadı.");

        // Kaçıncı kredi olduğunu kontrol et
        if (request.LoanNumber < 1 || request.LoanNumber > loans.Count)
            throw new Exception($"Geçersiz kredi numarası. Bu müşterinin {loans.Count} kredisi bulunmaktadır.");

        // LoanNumber 1'den başladığı için index = LoanNumber - 1
        var loan = loans[request.LoanNumber - 1];

        // Sıradaki ödenmemiş taksiti bul
        var installment = loan.Installments
            .Where(i => i.Status == InstallmentStatus.Unpaid)
            .OrderBy(i => i.InstallmentNumber)
            .FirstOrDefault();

        if (installment == null)
            throw new Exception($"{request.LoanNumber}. krediye ait ödenecek taksit bulunamadı. Tüm taksitler ödenmiş olabilir.");


        // Taksiti ödendi olarak işaretle
        installment.Status = InstallmentStatus.Paid;

        // Ödenmemiş taksit kalmadıysa krediyi kapat
        bool hasUnpaidInstallments = loan.Installments.Any(i => i.Status == InstallmentStatus.Unpaid);

        if (!hasUnpaidInstallments)
            loan.Status = LoanStatus.Closed;

        await _context.SaveChangesAsync();

        return new PaymentResponseDto
        {
            Message = $"{request.LoanNumber}. kredinin {installment.InstallmentNumber}. taksiti başarıyla ödendi.",
            CustomerId = loan.CustomerId,
            CustomerName = $"{loan.Customer.FirstName} {loan.Customer.LastName}",
            LoanId = loan.Id,
            LoanTypeName = GetLoanTypeName(loan.LoanType),
            InstallmentId = installment.Id,
            InstallmentNumber = installment.InstallmentNumber,
            PaidAmount = installment.Amount,
            IsLoanClosed = loan.Status == LoanStatus.Closed
        };
    }
    // Tüm kredileri müşteri bilgisi ve taksitleriyle birlikte DTO listesi olarak döner.
    public async Task<List<LoanResponseDto>> GetAllLoansDtoAsync()
    {
        var loans = await _context.Loans
            .Include(l => l.Customer)
            .Include(l => l.Installments)
            .ToListAsync();

        return loans.Select(loan => new LoanResponseDto
        {
            Id = loan.Id,
            CustomerId = loan.Customer.Id,
            CustomerFullName = $"{loan.Customer.FirstName} {loan.Customer.LastName}",
            LoanTypeName = GetLoanTypeName(loan.LoanType),
            Amount = loan.Amount,
            Tenor = loan.Tenor,
            ProfitRate = loan.ProfitRate,
            StartDate = loan.StartDate,
            Status = loan.Status.ToString(),
            Installments = loan.Installments.Select(i => new InstallmentDto
            {
                Id = i.Id,
                InstallmentNumber = i.InstallmentNumber,
                Amount = i.Amount,
                DueDate = i.DueDate,
                Status = i.Status.ToString()
            }).ToList()
        }).ToList();
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