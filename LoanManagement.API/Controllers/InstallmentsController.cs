using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LoanManagement.Business.Abstract;

namespace LoanManagement.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class InstallmentsController : ControllerBase
{
    private readonly IInstallmentService _installmentService;

    public InstallmentsController(IInstallmentService installmentService)
    {
        _installmentService = installmentService;
    }

    /// Krediye ait taksitleri listeler.
    [HttpGet("by-loan/{loanId}")]
    public async Task<IActionResult> GetByLoan(int loanId)
    {
        var installments = await _installmentService.GetByLoanIdAsync(loanId);
        return Ok(installments);
    }


    /// Taksit detayını getirir.
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var installment = await _installmentService.GetByIdAsync(id);
        if (installment == null) return NotFound(new { message = "Taksit bulunamadı." });
        return Ok(installment);
    }

    /// Müşterinin ödenmemiş taksitlerini listeler.
    [HttpGet("unpaid/by-customer/{customerId}")]
    public async Task<IActionResult> GetUnpaidByCustomer(int customerId)
    {
        var installments = await _installmentService.GetUnpaidByCustomerIdAsync(customerId);
        return Ok(installments);
    }

    /// Müşterinin gecikmiş taksitlerini listeler.
    [HttpGet("overdue/by-customer/{customerId}")]
    public async Task<IActionResult> GetOverdueByCustomer(int customerId)
    {
        var installments = await _installmentService.GetOverdueByCustomerIdAsync(customerId);
        return Ok(installments);
    }

    /// Vadesi geçmiş taksitleri "Gecikmiş" olarak günceller.
    [HttpPost("update-overdue")]
    public async Task<IActionResult> UpdateOverdue()
    {
        var count = await _installmentService.UpdateOverdueInstallmentsAsync();
        return Ok(new { message = $"{count} taksit gecikmiş olarak güncellendi.", updatedCount = count });
    }
}
