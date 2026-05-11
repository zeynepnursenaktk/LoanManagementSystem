namespace LoanManagement.Entities.Models;

public class Payment
{
    public int Id { get; set; }
    public int InstallmentId { get; set; }        // Hangi taksit ödendi?
    public decimal Amount { get; set; }           // Yatırılan tutar
    public DateTime PaymentDate { get; set; }     // Ödemenin yapıldığı tarih

    // İlişki
    public virtual Installment Installment { get; set; } = default!; //Bir taksitin en fazla bir ödemesi olabilir:
}