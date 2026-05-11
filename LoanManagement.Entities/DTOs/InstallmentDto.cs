namespace LoanManagement.Entities.DTOs;


// Taksit bilgilerini API'ye taşıyan veri transfer nesnesi.
public class InstallmentDto
{
    public int Id { get; set; }
    public int InstallmentNumber { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = null!;
}