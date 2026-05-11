namespace LoanManagement.Entities.DTOs;

public class PaymentResponseDto
{
    public string Message { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int LoanId { get; set; }
    public string LoanTypeName { get; set; } = string.Empty;
    public int InstallmentId { get; set; }
    public int InstallmentNumber { get; set; }
    public decimal PaidAmount { get; set; }
    public bool IsLoanClosed { get; set; }
}