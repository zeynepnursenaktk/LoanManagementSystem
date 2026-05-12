using Microsoft.EntityFrameworkCore;
using LoanManagement.Business.Abstract;
using LoanManagement.DataAccess.Context;
using LoanManagement.Entities.Models;
using LoanManagement.Entities.Enums;
using LoanManagement.Entities.DTOs;

namespace LoanManagement.Business.Services;

public class PaymentService : IPaymentService
{
    private readonly LoanDbContext _context;
    private readonly IMockPaymentGatewayService _paymentGateway;

    public PaymentService(LoanDbContext context, IMockPaymentGatewayService paymentGateway)
    {
        _context = context;
        _paymentGateway = paymentGateway;
    }

    /// Belirli bir taksit için ödeme yapar.
    /// Kurallar:
    /// - Bir ödeme yalnızca tek bir takside ait olabilir
    /// - Aynı taksit iki kere ödenemez
    /// - Ödeme yapıldığında taksit durumu "Ödendi" olur
    /// - Tüm taksitler ödendiyse kredi kapatılır
    public async Task<PaymentResponseDto> PayInstallmentAsync(PaymentRequestDto request)
    {
        var installment = await _context.Installments
            .Include(i => i.Loan)!.ThenInclude(l => l!.Customer)
            .Include(i => i.Loan)!.ThenInclude(l => l!.Installments)
            .Include(i => i.Payment)
            .FirstOrDefaultAsync(i => i.Id == request.InstallmentId);

        if (installment == null)
            throw new KeyNotFoundException("Taksit bulunamadı.");

        // Aynı taksitin iki kere ödenmesini engelle
        if (installment.Status == InstallmentStatus.Paid || installment.Payment != null)
            throw new InvalidOperationException("Bu taksit zaten ödenmiş. Aynı taksit tekrar ödenemez.");

        // Mock ödeme altyapısı ile ödeme işlemini gerçekleştir
        var gatewayResult = await _paymentGateway.ProcessPaymentAsync(
            installment.Amount,
            $"Kredi #{installment.LoanId} - Taksit #{installment.InstallmentNumber}"
        );

        if (!gatewayResult.IsSuccess)
            throw new InvalidOperationException($"Ödeme altyapısı hatası: {gatewayResult.Message}");

        // Transaction ile atomic işlem
        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            // Son kontrol (concurrency)
            if (installment.Status == InstallmentStatus.Paid)
                throw new InvalidOperationException("Bu taksit zaten ödenmiş.");

            // Taksiti ödendi olarak işaretle
            installment.Status = InstallmentStatus.Paid;

            // Ödeme kaydı oluştur
            var payment = new Payment
            {
                InstallmentId = installment.Id,
                Amount = installment.Amount,
                PaymentDate = DateTime.UtcNow
            };
            _context.Payments.Add(payment);

            // Tüm taksitler ödendiyse krediyi kapat
            var loan = installment.Loan!;
            bool allPaid = loan.Installments!.All(i =>
                i.Id == installment.Id || i.Status == InstallmentStatus.Paid);
            if (allPaid)
                loan.Status = LoanStatus.Closed;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return new PaymentResponseDto
            {
                Message = $"Taksit #{installment.InstallmentNumber} başarıyla ödendi. İşlem No: {gatewayResult.TransactionId}",
                PaymentId = payment.Id,
                CustomerId = loan.CustomerId,
                CustomerName = $"{loan.Customer!.FirstName} {loan.Customer.LastName}",
                LoanId = loan.Id,
                LoanTypeName = GetLoanTypeName(loan.LoanType),
                InstallmentId = installment.Id,
                InstallmentNumber = installment.InstallmentNumber,
                PaidAmount = payment.Amount,
                PaymentDate = payment.PaymentDate,
                IsLoanClosed = loan.Status == LoanStatus.Closed
            };
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<List<PaymentResponseDto>> GetAllPaymentsAsync()
    {
        var payments = await _context.Payments
            .Include(p => p.Installment)!.ThenInclude(i => i!.Loan)!.ThenInclude(l => l!.Customer)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

        return payments.Select(MapToDto).ToList();
    }

    public async Task<List<PaymentResponseDto>> GetPaymentsByCustomerIdAsync(int customerId)
    {
        var payments = await _context.Payments
            .Include(p => p.Installment)!.ThenInclude(i => i!.Loan)!.ThenInclude(l => l!.Customer)
            .Where(p => p.Installment!.Loan!.CustomerId == customerId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

        return payments.Select(MapToDto).ToList();
    }

    private static PaymentResponseDto MapToDto(Payment p)
    {
        return new PaymentResponseDto
        {
            PaymentId = p.Id,
            CustomerId = p.Installment!.Loan!.CustomerId,
            CustomerName = $"{p.Installment.Loan.Customer!.FirstName} {p.Installment.Loan.Customer.LastName}",
            LoanId = p.Installment.LoanId,
            LoanTypeName = GetLoanTypeName(p.Installment.Loan.LoanType),
            InstallmentId = p.InstallmentId,
            InstallmentNumber = p.Installment.InstallmentNumber,
            PaidAmount = p.Amount,
            PaymentDate = p.PaymentDate,
            Message = "Ödeme kaydı",
            IsLoanClosed = p.Installment.Loan.Status == LoanStatus.Closed
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
