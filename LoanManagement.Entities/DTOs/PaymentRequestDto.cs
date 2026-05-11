namespace LoanManagement.Entities.DTOs;

public class PaymentRequestDto
{
    public int CustomerId { get; set; }
    public int LoanNumber { get; set; }  // Müşterinin kaçıncı kredisi olduğu
}