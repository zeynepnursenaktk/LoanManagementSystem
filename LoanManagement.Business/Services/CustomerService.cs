using Microsoft.EntityFrameworkCore;
using LoanManagement.Business.Abstract;
using LoanManagement.DataAccess.Context;
using LoanManagement.Entities.Models;

namespace LoanManagement.Business.Services;

public class CustomerService : ICustomerService
{
    private readonly LoanDbContext _context;

    public CustomerService(LoanDbContext context)
    {
        _context = context;
    }

    // Tüm müşterileri getir
    public async Task<List<Customer>> GetAllAsync()
    {
        return await _context.Customers.ToListAsync();
    }

    // ID'ye göre tek müşteri getir (kredileriyle birlikte)
    public async Task<Customer?> GetByIdAsync(int id)
    {
        return await _context.Customers
            .Include(c => c.Loans) // Müşterinin kredilerini de getir
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    // Yeni müşteri ekle
    public async Task AddAsync(Customer customer)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
    }

    // Müşteri güncelle
    public async Task UpdateAsync(Customer customer)
    {
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();
    }

    // Müşteri sil
    public async Task DeleteAsync(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer != null)
        {
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
        }
    }
}