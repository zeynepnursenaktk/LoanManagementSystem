using LoanManagement.Entities.Models;
using LoanManagement.Entities.DTOs;

namespace LoanManagement.Business.Abstract;

public interface ICustomerService
{
    Task AddAsync(Customer customer);
    Task<List<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(int id);
    Task<CustomerSummaryDto?> GetCustomerSummaryAsync(int customerId);
    Task<bool> UpdateAsync(int id, UpdateCustomerDto dto);
}