namespace LoanManagement.Entities.DTOs;

public class CustomerListDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public int TotalLoans { get; set; }
    public List<LoanSummaryDto> Loans { get; set; } = new();
}
