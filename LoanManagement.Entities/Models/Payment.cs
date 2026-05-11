namespace LoanManagement.Entities.Models;

public class Payment
{
    public int Id { get; set; }
    public int InstallmentId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }

    /// <summary>Ödemenin ait olduğu taksit</summary>
    public virtual Installment? Installment { get; set; }
}