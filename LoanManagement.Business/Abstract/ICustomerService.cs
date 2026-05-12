using LoanManagement.Entities.DTOs;

namespace LoanManagement.Business.Abstract;

public interface ICustomerService
{
    Task<List<CustomerListDto>> GetAllAsync();
    Task<CustomerResponseDto?> GetByIdAsync(int id);
    Task<int> CreateAsync(CreateCustomerDto dto);
    Task<bool> UpdateAsync(int id, UpdateCustomerDto dto);
    Task<bool> DeleteAsync(int id);
    Task<CustomerSummaryDto?> GetCustomerSummaryAsync(int customerId);
}
