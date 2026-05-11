using LoanManagement.Entities.Enums;

namespace LoanManagement.Entities.Models;

// Taksitler kredi oluşturulurken otomatik olarak hesaplanır ve oluşturulur.
public class Loan
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }       //Kredi ana para tutarı
    public int Tenor { get; set; }            //Vade süresi (ay cinsinden)
    public decimal ProfitRate { get; set; }   //Kar oranı (örn: 0.20 = %20)
    public DateTime StartDate { get; set; }
    public LoanStatus Status { get; set; }    //Kredinin mevcut durumu (Active, Closed)
    public LoanType LoanType { get; set; }

    public virtual Customer? Customer { get; set; } = null!; //İlişkili müşteri nesnesi (Navigation Property)
    public virtual ICollection<Installment>? Installments { get; set; } = new List<Installment>(); //Krediye ait taksit kayıtları (Navigation Property)
}