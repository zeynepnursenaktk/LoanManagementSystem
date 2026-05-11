namespace LoanManagement.Entities.Models;

public class Customer
{
    public int Id { get; set; }
    public string IdentityNumber { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;

    public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();
}