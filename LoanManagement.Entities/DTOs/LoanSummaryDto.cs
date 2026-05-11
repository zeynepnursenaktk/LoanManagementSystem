namespace LoanManagement.Entities.DTOs;


// Müşteri detayında kredileri özet olarak göstermek için kullanılan DTO.
// Taksit detayları bu DTO'da yer almaz, sadece kredi özeti sunulur.
public class LoanSummaryDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public int Tenor { get; set; }
    public string Status { get; set; } = null!;
    public DateTime StartDate { get; set; }
}