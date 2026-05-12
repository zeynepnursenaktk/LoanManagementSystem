namespace LoanManagement.Entities.Models;

public class Customer
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string IdentityNumber { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public virtual ICollection<Loan>? Loans { get; set; } = new List<Loan>(); //müşteriye ait kredi kayıtları (Navigation Property)
}