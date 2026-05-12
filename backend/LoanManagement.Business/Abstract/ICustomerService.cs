using LoanManagement.Entities.DTOs;

namespace LoanManagement.Business.Abstract;

public interface ICustomerService
{
    /// <summary>
    /// Müşteri listesini döner.
    /// </summary>
    /// <param name="includeDeleted">
    /// <c>true</c> ise soft-delete edilmiş müşteriler de döner (Admin paneli için).
    /// <c>false</c> (varsayılan) yalnızca aktif müşterileri döner.
    /// </param>
    Task<List<CustomerListDto>> GetCustomersAsync(bool includeDeleted = false);

    Task<CustomerResponseDto?> GetCustomerByIdAsync(int customerId);

    Task<int> CreateCustomerAsync(CreateCustomerDto dto);

    Task<bool> UpdateCustomerAsync(int customerId, UpdateCustomerDto dto);

    Task<bool> DeleteCustomerAsync(int customerId);

    /// <summary>
    /// Soft-delete edilmiş müşteriyi yeniden aktifleştirir.
    /// </summary>
    /// <returns>
    /// <c>true</c>: restore başarılı. <c>false</c>: müşteri bulunamadı veya zaten aktif.
    /// </returns>
    Task<bool> RestoreCustomerAsync(int customerId);

    Task<CustomerSummaryDto?> GetCustomerSummaryAsync(int customerId);
}
