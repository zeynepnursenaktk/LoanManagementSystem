public class LoanSummaryDto
{
    public int LoanId { get; set; }
    public string LoanTypeName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public decimal Amount { get; set; }
    public int Tenor { get; set; }
    public DateTime? StartDate { get; set; }
    public int TotalInstallments { get; set; }
    public int PaidInstallments { get; set; }
    public int UnpaidInstallments { get; set; }
    public decimal RemainingDebt { get; set; }
}