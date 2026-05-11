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


    // Yeni bir kredi oluşturur ve vadeye göre taksitleri otomatik üretir.
    // Validasyon → Hesaplama → Taksit üretimi → DB kayıt sırasıyla çalışır.
    public async Task CreateLoanWithInstallmentsAsync(Loan loan)
    {
        // Vade 0 veya negatif olamaz
        if (loan.Tenor <= 0)
            throw new Exception($"Vade (Tenor) değeri 0 olamaz! Gelen değer: {loan.Tenor}");

        // Kredi tutarı 0 veya negatif olamaz
        if (loan.Amount <= 0)
            throw new Exception($"Kredi tutarı (Amount) 0 olamaz! Gelen değer: {loan.Amount}");

        // Sadece tanımlı 3 kredi türü kabul edilir: Personal, Education, Vehicle
        if (loan.LoanType != LoanType.Personal &&
            loan.LoanType != LoanType.Education &&
            loan.LoanType != LoanType.Vehicle)
        {
            throw new Exception("Geçersiz kredi türü! 0: İhtiyaç, 1: Eğitim, 2: Araç");
        }

        // Mock kredi skoru kontrolü
        int creditScore = await _creditScoreService.GetCreditScoreAsync(loan.CustomerId);

        if (creditScore < 600)
            throw new Exception($"Kredi başvurusu reddedildi. Kredi skoru yetersiz: {creditScore}");

        // Toplam geri ödenecek tutar = Ana Para + (Ana Para × Kar Oranı)
        decimal totalPayable = loan.Amount + (loan.Amount * loan.ProfitRate);

        // Aylık taksit tutarı = Toplam tutar / Vade
        decimal monthlyAmount = totalPayable / loan.Tenor;

        // Kredi oluşturulduğu anda aktif statüsüne geçer
        loan.Status = LoanStatus.Active;

        // Her ay için bir taksit kaydı oluşturulur
        for (int i = 1; i <= loan.Tenor; i++)
        {
            var installment = new Installment
            {
                InstallmentNumber = i,
                Amount = monthlyAmount,

                // Taksit vadesi: Kredi başlangıç tarihinden itibaren aylık artar
                DueDate = loan.StartDate.AddMonths(i),
                Status = InstallmentStatus.Unpaid // Henüz ödenmedi
            };

            // EF Core, taksit ile kredi arasındaki FK ilişkisini otomatik kurar
            loan.Installments.Add(installment);
        }

        // Kredi ve tüm taksitler tek seferde veritabanına yazılır
        _context.Loans.Add(loan);
        await _context.SaveChangesAsync();
    }


    // ID'ye göre tek bir krediyi taksitleri ve müşteri bilgisiyle birlikte getirir.
    public async Task<LoanResponseDto?> GetLoanByIdDtoAsync(int id)
    {
        var loan = await _context.Loans
            .Include(l => l.Installments)
            .Include(l => l.Customer)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (loan == null) return null;

        // Veritabanındaki kredi türü enum dışında bir değerse hata fırlat
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

    // Taksit ödeme işlemini gerçekleştirir.
    // Sıradaki ödenmemiş taksiti otomatik bulur, atlama yapılamaz.
    // Tüm taksitler ödenirse kredi otomatik olarak "Closed" statüsüne geçer.
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

    // Kredi türü enum değerini Türkçe isme çevirir.
    // Tekrar eden switch ifadelerini tek noktada toplamak için private yardımcı metod.
    private static string GetLoanTypeName(LoanType loanType) => loanType switch
    {
        LoanType.Personal => "İhtiyaç Kredisi",
        LoanType.Education => "Eğitim Kredisi",
        LoanType.Vehicle => "Araç Kredisi",
        _ => "Bilinmeyen Kredi Türü"
    };
}