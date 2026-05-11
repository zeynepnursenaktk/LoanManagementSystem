using LoanManagement.Entities.Enums;

public class LoanRequestDto
{
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public int Tenor { get; set; }
    public decimal ProfitRate { get; set; }
    public DateTime StartDate { get; set; }
    public LoanType LoanType { get; set; }
}