using Microsoft.EntityFrameworkCore;
using LoanManagement.Business.Abstract;
using LoanManagement.Business.ExternalServices;
using LoanManagement.DataAccess.Context;
using LoanManagement.Entities.Models;
using LoanManagement.Entities.Enums;
using LoanManagement.Entities.DTOs;

namespace LoanManagement.Business.Services;

public class PaymentService : IPaymentService
{
    private readonly LoanDbContext _context;
    private readonly IExternalPaymentGatewayService _paymentGateway;

    public PaymentService(LoanDbContext context, IExternalPaymentGatewayService paymentGateway)
    {
        _context = context;
        _paymentGateway = paymentGateway; // dış ödeme sağlayıcısı (Stripe Sandbox / iyzico Sandbox)
    }

    /// Belirtilen kredide sıradaki (en küçük numaralı, henüz ödenmemiş) taksi için ödeme yapar.
    /// Kurallar: önce 1. taksit, sonra 2. … sıra zorunlu; aynı taksit iki kez ödenemez.
    /// Dış sağlayıcı reddederse <see cref="PaymentDeclinedException"/> fırlatır; controller 402 döner.
    public async Task<PaymentResponseDto> PayInstallmentAsync(PaymentRequestDto request, int? scopedCustomerId = null)
    {
        var loan = await _context.Loans
            .Include(l => l.Installments)!.ThenInclude(i => i.Payment)
            .Include(l => l.Customer)
            .FirstOrDefaultAsync(l => l.Id == request.LoanId);

        if (loan == null)
            throw new KeyNotFoundException("Kredi bulunamadı.");

        if (scopedCustomerId is int cid && loan.CustomerId != cid)
            throw new UnauthorizedAccessException("Bu kredi için ödeme yetkiniz yok.");

        var installment = loan.Installments!
            .OrderBy(i => i.InstallmentNumber)
            .FirstOrDefault(i => i.Status != InstallmentStatus.Paid && i.Payment == null);

        if (installment == null)
            throw new InvalidOperationException("Bu kredi için ödenecek taksit kalmadı veya tüm taksitler zaten ödendi.");

        if (installment.Status == InstallmentStatus.Paid || installment.Payment != null)
            throw new InvalidOperationException("Bu taksit zaten ödenmiş.");

        // Idempotency key: aynı taksit için yapılan tüm denemeler aynı transaction kabul edilir.
        var idempotencyKey = $"loan-{loan.Id}-inst-{installment.Id}";
        var metadata = new Dictionary<string, string>
        {
            ["loanId"] = loan.Id.ToString(),
            ["installmentId"] = installment.Id.ToString(),
            ["installmentNumber"] = installment.InstallmentNumber.ToString(),
            ["customerId"] = loan.CustomerId.ToString(),
        };

        var gatewayResult = await _paymentGateway.ChargeAsync(
            installment.Amount,
            $"Kredi #{loan.Id} - Taksit #{installment.InstallmentNumber}",
            idempotencyKey,
            metadata
        );

        if (!gatewayResult.IsSuccess)
        {
            // Reddedildi / Hata — DB'ye yazma, exception fırlat, controller 402 döner.
            throw new PaymentDeclinedException(
                declineCode: gatewayResult.DeclineCode ?? PaymentDeclineCodes.ProcessingError,
                providerName: gatewayResult.ProviderName,
                message: gatewayResult.Message,
                status: gatewayResult.Status);
        }

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.Entry(installment).ReloadAsync();
            if (installment.Status == InstallmentStatus.Paid
                || await _context.Payments.AnyAsync(p => p.InstallmentId == installment.Id))
                throw new InvalidOperationException("Bu taksit başka bir işlemle ödenmiş; lütfen tekrar deneyin.");

            installment.Status = InstallmentStatus.Paid;

            var payment = new Payment
            {
                InstallmentId = installment.Id,
                Amount = installment.Amount,
                PaymentDate = DateTime.UtcNow
            };
            _context.Payments.Add(payment);

            bool allPaid = loan.Installments!.All(i => i.Status == InstallmentStatus.Paid);
            if (allPaid)
                loan.Status = LoanStatus.Closed;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return new PaymentResponseDto
            {
                Status = "Succeeded",
                DeclineCode = null,
                ProviderName = gatewayResult.ProviderName,
                ProviderReference = gatewayResult.ProviderReference,
                Message = $"Kredi #{loan.Id} — Taksit #{installment.InstallmentNumber} başarıyla ödendi.",
                PaymentId = payment.Id,
                CustomerId = loan.CustomerId,
                CustomerName = $"{loan.Customer!.FirstName} {loan.Customer.LastName}",
                LoanId = loan.Id,
                LoanTypeName = GetLoanTypeName(loan.LoanType),
                InstallmentId = installment.Id,
                InstallmentNumber = installment.InstallmentNumber,
                PaidAmount = payment.Amount,
                PaymentDate = payment.PaymentDate,
                ProcessedAtUtc = gatewayResult.ProcessedAtUtc,
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
            Status = "Succeeded",
            ProviderName = string.Empty,
            ProviderReference = string.Empty,
            PaymentId = p.Id,
            CustomerId = p.Installment!.Loan!.CustomerId,
            CustomerName = $"{p.Installment.Loan.Customer!.FirstName} {p.Installment.Loan.Customer.LastName}",
            LoanId = p.Installment.LoanId,
            LoanTypeName = GetLoanTypeName(p.Installment.Loan.LoanType),
            InstallmentId = p.InstallmentId,
            InstallmentNumber = p.Installment.InstallmentNumber,
            PaidAmount = p.Amount,
            PaymentDate = p.PaymentDate,
            ProcessedAtUtc = p.PaymentDate,
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
