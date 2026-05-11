namespace LoanManagement.Entities.DTOs;

using LoanManagement.Entities.Enums;

public class CreateLoanDto
{
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public int Tenor { get; set; }
    public decimal ProfitRate { get; set; }
    public LoanType LoanType { get; set; }
}