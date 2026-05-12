using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LoanManagement.Business.Abstract;
using LoanManagement.Entities.DTOs;
using LoanManagement.API.Security;
using LoanManagement.Entities;

namespace LoanManagement.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class LoansController : ControllerBase
{
    private readonly ILoanService _loanService;
    private readonly ICurrentUserAccessor _current;

    public LoansController(ILoanService loanService, ICurrentUserAccessor current)
    {
        _loanService = loanService;
        _current = current;
    }

    /// Yeni kredi oluşturur ve taksit planını otomatik hesaplar.
    [HttpPost]
    public async Task<IActionResult> CreateLoan([FromBody] LoanRequestDto dto)
    {
        if (!_current.IsAdmin)
        {
            if (_current.CustomerId is not int myCid)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Kredi oluşturmak için müşteri hesabı gereklidir." });
            dto.CustomerId = myCid;
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(new
            {
                message = "Giriş verisi validasyonu başarısız.",
                errors
            });
        }

        try
        {
            int loanId = await _loanService.CreateLoanAsync(dto);
            var loan = await _loanService.GetLoanByIdAsync(loanId);
            return CreatedAtAction(nameof(GetLoanById), new { id = loanId }, loan);
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

    /// Tüm kredileri listeler (Admin); müşteri yalnızca kendi kredilerini görür.
    [HttpGet]
    public async Task<IActionResult> GetLoans()
    {
        if (_current.IsAdmin)
        {
            var all = await _loanService.GetLoansAsync();
            return Ok(all);
        }

        if (_current.CustomerId is not int cid)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Liste için müşteri kimliği gereklidir." });

        var own = await _loanService.GetLoansByCustomerIdAsync(cid);
        return Ok(own);
    }

    /// ID'ye göre kredi detayını getirir.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetLoanById(int id)
    {
        if (id <= 0)
            return BadRequest(new { message = "Geçersiz kredi ID. ID 0'dan büyük olmalıdır." });

        var loan = await _loanService.GetLoanByIdAsync(id);
        if (loan == null) return NotFound(new { message = "Kredi bulunamadı." });

        if (!_current.IsAdmin)
        {
            if (_current.CustomerId is not int cid || loan.CustomerId != cid)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Bu krediye erişim yetkiniz yok." });
        }

        return Ok(loan);
    }

    /// Müşteriye ait kredileri listeler.
    [HttpGet("by-customer/{customerId:int}")]
    public async Task<IActionResult> GetLoansByCustomerId(int customerId)
    {
        if (customerId <= 0)
            return BadRequest(new { message = "Geçersiz müşteri ID. ID 0'dan büyük olmalıdır." });

        if (!_current.IsAdmin)
        {
            if (_current.CustomerId is not int cid || cid != customerId)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Bu müşterinin kredilerini görüntüleme yetkiniz yok." });
        }

        var loans = await _loanService.GetLoansByCustomerIdAsync(customerId);
        return Ok(loans);
    }
}
