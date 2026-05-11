namespace LoanManagement.Entities.Models;

public class Payment
{
    public int Id { get; set; }
    public int InstallmentId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }

    public virtual Installment? Installment { get; set; }     //Ödemenin ait olduğu taksitD
}