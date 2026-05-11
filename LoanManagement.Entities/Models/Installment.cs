using LoanManagement.Entities.Enums;

namespace LoanManagement.Entities.Models;

public class Installment
{
    public int Id { get; set; }
    public int LoanId { get; set; }
    public int InstallmentNumber { get; set; }// Kaçıncı taksit? (1, 2, 3...)
    public decimal Amount { get; set; } // Taksit tutarı
    public DateTime DueDate { get; set; }  // Son ödeme tarihi
    public InstallmentStatus Status { get; set; } // Ödenme durumu


    public virtual Loan Loan { get; set; } = default!;
    public virtual Payment? Payment { get; set; } // Ödeme henüz yapılmamış olabilir, o yüzden ? ekledik
}