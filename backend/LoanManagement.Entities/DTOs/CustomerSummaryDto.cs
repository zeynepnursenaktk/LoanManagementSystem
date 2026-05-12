namespace LoanManagement.Entities.DTOs;

public class CustomerSummaryDto
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int TotalLoans { get; set; }
    public int ActiveLoans { get; set; }
    public int ClosedLoans { get; set; }
    public decimal TotalDebt { get; set; }
    public decimal TotalPaid { get; set; }
    public int TotalInstallments { get; set; }
    public int PaidInstallments { get; set; }
    public int UnpaidInstallments { get; set; }
    public int OverdueInstallments { get; set; }  // Gecikmiş taksit sayısı

    public List<LoanDetailSummaryDto> Loans { get; set; } = new();
}
