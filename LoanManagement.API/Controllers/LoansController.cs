using Microsoft.AspNetCore.Mvc;
using LoanManagement.Business.Abstract;
using LoanManagement.Entities.DTOs;

namespace LoanManagement.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class LoansController : ControllerBase
{
    private readonly ILoanService _loanService;

    public LoansController(ILoanService loanService)
    {
        _loanService = loanService;
    }

    // Yeni bir kredi oluşturur ve taksit planını otomatik olarak oluşturur.
    [HttpPost]
    public async Task<IActionResult> CreateLoan([FromBody] LoanRequestDto dto)
    {
        try
        {
            int loanId = await _loanService.CreateLoanWithInstallmentsAsync(dto);
            
            var createdLoanDto = await _loanService.GetLoanByIdDtoAsync(loanId);
            return CreatedAtAction(nameof(GetById), new { id = loanId }, createdLoanDto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Sunucu hatası: " + ex.Message });
        }
    }

    // Belirtilen ID'ye sahip kredinin detaylarını getirir.
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var loanDto = await _loanService.GetLoanByIdDtoAsync(id);

        if (loanDto == null)
            return NotFound(new { message = "Kredi bulunamadı." });

        return Ok(loanDto);
    }

    // Bir müşterinin sıradaki taksidini öder.
    [HttpPost("pay-installment")]
    public async Task<IActionResult> PayInstallment([FromBody] PaymentRequestDto request)
    {
        try
        {
            var result = await _loanService.PayInstallmentAsync(request);

            if (result == null)
                return NotFound(new { message = "Taksit bulunamadı." });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Tüm kredileri müşteri ve taksit bilgileriyle birlikte getirir.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var loans = await _loanService.GetAllLoansDtoAsync();
        return Ok(loans);
    }
}