// Business/DTOs/PaymentResponseDto.cs
public class PaymentResponseDto
{
    public string Message { get; set; } = null!;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = null!;
    public int LoanId { get; set; }
    public string LoanTypeName { get; set; } = null!;
    public int InstallmentId { get; set; }
    public int InstallmentNumber { get; set; }
    public decimal PaidAmount { get; set; }
    public bool IsLoanClosed { get; set; }
}