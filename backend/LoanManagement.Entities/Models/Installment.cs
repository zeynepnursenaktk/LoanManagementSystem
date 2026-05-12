using LoanManagement.Entities.Enums;

namespace LoanManagement.Entities.Models;


// Taksitler kredi oluşturulurken otomatik olarak hesaplanır ve oluşturulur.
public class Installment
{
    public int Id { get; set; }
    public int LoanId { get; set; }
    public int InstallmentNumber { get; set; }
    public decimal Amount { get; set; }  //Taksit tutarı (Toplam geri ödeme / Vade)
    public DateTime DueDate { get; set; } //Taksitin son ödeme tarihi
    public InstallmentStatus Status { get; set; } //Taksitin mevcut durumu (Unpaid, Paid, Overdue)

    public virtual Payment? Payment { get; set; }
    public virtual Loan? Loan { get; set; } = null!; //İlişkili kredi nesnesi (Navigation Property)
}