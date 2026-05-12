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

    /// <summary>
    /// Müşteri soft-delete edilmişse <c>true</c> döner.
    /// Admin paneli "Aktif/Pasif" badge'i için kullanılır.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Soft-delete zamanı (UTC). <see cref="IsDeleted"/> <c>false</c> ise <c>null</c>'dur.
    /// </summary>
    public DateTime? DeletedAtUtc { get; set; }
}
