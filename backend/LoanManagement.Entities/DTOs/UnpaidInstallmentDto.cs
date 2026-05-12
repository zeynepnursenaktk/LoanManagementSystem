namespace LoanManagement.Entities.DTOs;

public class UnpaidInstallmentDto
{
    public int Id { get; set; }
    public int LoanId { get; set; }
    public int InstallmentNumber { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
}