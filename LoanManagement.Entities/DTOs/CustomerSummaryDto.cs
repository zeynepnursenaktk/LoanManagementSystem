namespace LoanManagement.Entities.DTOs;

public class CustomerSummaryDto
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int TotalLoans { get; set; }          // Toplam kredi sayısı
    public int ActiveLoans { get; set; }          // Aktif kredi sayısı
    public int ClosedLoans { get; set; }          // Kapatılmış kredi sayısı
    public decimal TotalDebt { get; set; }        // Toplam kalan borç
    public decimal TotalPaid { get; set; }        // Toplam ödenen tutar
    public int TotalInstallments { get; set; }    // Toplam taksit sayısı
    public int PaidInstallments { get; set; }     // Ödenen taksit sayısı
    public int UnpaidInstallments { get; set; }   // Kalan taksit sayısı

    // Her kredi ayrı ayrı detaylı gösterilir
    public List<LoanDetailSummaryDto> Loans { get; set; } = new();
}