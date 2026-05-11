using LoanManagement.Entities.DTOs;

public interface ICustomerService
{
    Task<List<CustomerListDto>> GetAllAsync();

    Task<CustomerResponseDto?> GetByIdAsync(int id);

    Task<int> CreateAsync(CreateCustomerDto dto);

    Task<bool> UpdateAsync(int id, UpdateCustomerDto dto);

    Task<CustomerSummaryDto?> GetCustomerSummaryAsync(int customerId);
}