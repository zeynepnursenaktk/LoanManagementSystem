using Microsoft.AspNetCore.Mvc;
using LoanManagement.Business.Abstract;
using LoanManagement.Entities.Models;

namespace LoanManagement.API.Controllers;

[Route("api/[controller]")]
[ApiController] // Bu attribute, gelen verilerin otomatik doğrulanmasını sağlar
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet] // Tüm müşterileri listeler
    public async Task<IActionResult> GetAll()
    {
        var customers = await _customerService.GetAllAsync();
        return Ok(customers);
    }

    [HttpGet("{id}")] // ID'ye göre müşteri getirir
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer == null) return NotFound("Müşteri bulunamadı.");
        return Ok(customer);
    }

    [HttpPost] // Yeni müşteri ekler
    public async Task<IActionResult> Create(Customer customer)
    {
        await _customerService.AddAsync(customer);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }
}