using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LoanManagement.Business.Abstract;
using LoanManagement.Entities.DTOs;

namespace LoanManagement.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    /// Tüm müşterileri listeler.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var customers = await _customerService.GetAllAsync();
        return Ok(customers);
    }

    /// ID'ye göre müşteri detayını getirir.
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer == null) return NotFound(new { message = "Müşteri bulunamadı." });
        return Ok(customer);
    }

    /// Yeni müşteri oluşturur.
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
    {
        try
        {
            var id = await _customerService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id }, new { id, message = "Müşteri başarıyla oluşturuldu." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// Müşteri bilgilerini günceller.
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerDto dto)
    {
        var ok = await _customerService.UpdateAsync(id, dto);
        if (!ok) return NotFound(new { message = "Müşteri bulunamadı." });
        return Ok(new { message = "Müşteri başarıyla güncellendi." });
    }

    /// Müşteriyi siler. Aktif kredisi olan müşteri silinemez.
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var ok = await _customerService.DeleteAsync(id);
            if (!ok) return NotFound(new { message = "Müşteri bulunamadı." });
            return Ok(new { message = "Müşteri başarıyla silindi." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// Müşterinin borç ve kredi özetini getirir.
    [HttpGet("{id}/summary")]
    public async Task<IActionResult> GetSummary(int id)
    {
        var summary = await _customerService.GetCustomerSummaryAsync(id);
        if (summary == null) return NotFound(new { message = "Müşteri bulunamadı." });
        return Ok(summary);
    }
}
