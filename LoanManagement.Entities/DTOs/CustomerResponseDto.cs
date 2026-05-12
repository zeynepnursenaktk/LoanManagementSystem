namespace LoanManagement.Entities.DTOs;

public class CustomerResponseDto : CustomerListDto
{
    public string IdentityNumber { get; set; } = null!;
}
