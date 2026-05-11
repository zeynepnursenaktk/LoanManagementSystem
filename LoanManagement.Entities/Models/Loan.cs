using LoanManagement.Entities.Enums;

namespace LoanManagement.Entities.Models;

public class Loan
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }           // Çekilen toplam tutar
    public decimal InterestRate { get; set; }     // Kar oranı (Örn: 1.20)
    public int Tenor { get; set; }                // Vade (Ay sayısı)
    public DateTime StartDate { get; set; }       // Kredi başlangıç tarihi
    public LoanType LoanType { get; set; }        // İhtiyaç/Eğitim/Taşıt
    public LoanStatus Status { get; set; }        // Aktif/Kapalı


    // İlişkiler
    public virtual Customer Customer { get; set; } = default!;
    public virtual ICollection<Installment> Installments { get; set; } = new List<Installment>(); //Bir kredinin birden fazla taksiti olabilir:
}

