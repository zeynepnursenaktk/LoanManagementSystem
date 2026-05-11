namespace LoanManagement.Entities.DTOs
{
    public class PaymentRequestDto
    {
        public int CustomerId { get; set; }
        public int LoanNumber { get; set; } // müşteri için sıralı kredi numarası (1,2,...)
    }
}