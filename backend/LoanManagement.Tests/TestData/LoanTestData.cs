using LoanManagement.Entities.DTOs;
using LoanManagement.Entities.Enums;

namespace LoanManagement.Tests.TestData;

public static class LoanTestData
{
    public static LoanRequestDto ValidLoanRequestDto(int customerId = 1) => new()
    {
        CustomerId = customerId,
        Amount = 100_000m,
        Tenor = 12,
        ProfitRate = 1.8m,
        StartDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Unspecified),
        LoanType = LoanType.Personal
    };

    public static LoanResponseDto ValidLoanResponseDto(int loanId = 1, int customerId = 1) => new()
    {
        Id = loanId,
        CustomerId = customerId,
        CustomerFullName = "Test User",
        LoanTypeName = "İhtiyaç Kredisi",
        Amount = 100_000m,
        Tenor = 12,
        ProfitRate = 1.8m,
        TotalPayable = 101_800m,
        StartDate = new DateTime(2026, 6, 1),
        Status = "Active",
        Installments = new List<InstallmentDto>()
    };
}
