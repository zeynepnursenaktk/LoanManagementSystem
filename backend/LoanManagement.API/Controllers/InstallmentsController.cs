using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LoanManagement.Business.Abstract;
using LoanManagement.API.Security;
using LoanManagement.Entities;

namespace LoanManagement.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class InstallmentsController : ControllerBase
{
    private readonly IInstallmentService _installmentService;
    private readonly ICurrentUserAccessor _current;

    public InstallmentsController(IInstallmentService installmentService, ICurrentUserAccessor current)
    {
        _installmentService = installmentService;
        _current = current;
    }

    /// Krediye ait taksitleri listeler.
    [HttpGet("by-loan/{loanId}")]
    public async Task<IActionResult> GetByLoan(int loanId)
    {
        if (!_current.IsAdmin)
        {
            if (_current.CustomerId is not int cid)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Bu işlem için müşteri hesabı gereklidir." });
            if (!await _installmentService.LoanBelongsToCustomerAsync(loanId, cid))
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Bu kredinin taksitlerini görüntüleme yetkiniz yok." });
        }

        var installments = await _installmentService.GetByLoanIdAsync(loanId);
        return Ok(installments);
    }

    /// Taksit detayını getirir.
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!_current.IsAdmin)
        {
            if (_current.CustomerId is not int cid)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Bu işlem için müşteri hesabı gereklidir." });
            if (!await _installmentService.InstallmentBelongsToCustomerAsync(id, cid))
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Bu takside erişim yetkiniz yok." });
        }

        var installment = await _installmentService.GetByIdAsync(id);
        if (installment == null) return NotFound(new { message = "Taksit bulunamadı." });
        return Ok(installment);
    }

    /// Müşterinin ödenmemiş taksitlerini listeler.
    [HttpGet("unpaid/by-customer/{customerId}")]
    public async Task<IActionResult> GetUnpaidByCustomer(int customerId)
    {
        if (!_current.IsAdmin)
        {
            if (_current.CustomerId is not int cid || cid != customerId)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Bu müşterinin taksitlerini görüntüleme yetkiniz yok." });
        }

        var installments = await _installmentService.GetUnpaidByCustomerIdAsync(customerId);
        return Ok(installments);
    }

    /// Müşterinin gecikmiş taksitlerini listeler.
    [HttpGet("overdue/by-customer/{customerId}")]
    public async Task<IActionResult> GetOverdueByCustomer(int customerId)
    {
        if (!_current.IsAdmin)
        {
            if (_current.CustomerId is not int cid || cid != customerId)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Bu müşterinin taksitlerini görüntüleme yetkiniz yok." });
        }

        var installments = await _installmentService.GetOverdueByCustomerIdAsync(customerId);
        return Ok(installments);
    }

    /// Vadesi geçmiş taksitleri "Gecikmiş" olarak günceller (yalnızca Admin).
    [HttpPost("update-overdue")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> UpdateOverdue()
    {
        var count = await _installmentService.UpdateOverdueInstallmentsAsync();
        return Ok(new { message = $"{count} taksit gecikmiş olarak güncellendi.", updatedCount = count });
    }
}
