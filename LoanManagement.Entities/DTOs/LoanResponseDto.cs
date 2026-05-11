namespace LoanManagement.Entities.DTOs;


// Kredi detay sorgularında API'den dönen veri transfer nesnesi.
public class LoanResponseDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public int Tenor { get; set; }
    public decimal ProfitRate { get; set; }
    public DateTime StartDate { get; set; }
    public string Status { get; set; } = null!;
    public List<InstallmentDto> Installments { get; set; } = new();
}