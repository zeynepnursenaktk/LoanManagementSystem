using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LoanManagement.Business.Abstract;
using LoanManagement.Entities.DTOs;

namespace LoanManagement.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class LoansController : ControllerBase
{
    private readonly ILoanService _loanService;

    public LoansController(ILoanService loanService)
    {
        _loanService = loanService;
    }

    /// Yeni kredi oluşturur ve taksit planını otomatik hesaplar.
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LoanRequestDto dto)
    {
        try
        {
            int loanId = await _loanService.CreateLoanWithInstallmentsAsync(dto);
            var loan = await _loanService.GetLoanByIdDtoAsync(loanId);
            return CreatedAtAction(nameof(GetById), new { id = loanId }, loan);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// Tüm kredileri listeler.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var loans = await _loanService.GetAllLoansDtoAsync();
        return Ok(loans);
    }

    /// ID'ye göre kredi detayını getirir.
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var loan = await _loanService.GetLoanByIdDtoAsync(id);
        if (loan == null) return NotFound(new { message = "Kredi bulunamadı." });
        return Ok(loan);
    }

    /// Müşteriye ait kredileri listeler.
    [HttpGet("by-customer/{customerId}")]
    public async Task<IActionResult> GetByCustomer(int customerId)
    {
        var loans = await _loanService.GetLoansByCustomerIdAsync(customerId);
        return Ok(loans);
    }
}
