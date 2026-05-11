public class CreateCustomerDto
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string IdentityNumber { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
}

public class UpdateCustomerDto
{
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
}