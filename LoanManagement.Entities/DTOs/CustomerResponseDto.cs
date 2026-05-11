namespace LoanManagement.Entities.DTOs;


// Müşteri detay sorgularında API'den dönen veri transfer nesnesi.
public class CustomerResponseDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string IdentityNumber { get; set; } = null!;
    public List<LoanSummaryDto> Loans { get; set; } = new();
}