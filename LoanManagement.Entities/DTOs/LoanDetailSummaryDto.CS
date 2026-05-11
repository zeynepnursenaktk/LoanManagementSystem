namespace LoanManagement.Entities.DTOs;

public class LoanDetailSummaryDto
{
    public int LoanId { get; set; }
    public string LoanTypeName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }           // Ana para
    public decimal TotalPayable { get; set; }     // Toplam ödenecek (ana para + kar)
    public decimal TotalPaid { get; set; }        // Bu krediden ödenen
    public decimal RemainingDebt { get; set; }    // Bu krediden kalan borç
    public int TotalInstallments { get; set; }
    public int PaidInstallments { get; set; }
    public int UnpaidInstallments { get; set; }
}