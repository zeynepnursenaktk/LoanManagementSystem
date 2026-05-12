using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LoanManagement.Business.Abstract;
using LoanManagement.Entities.DTOs;

namespace LoanManagement.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// Taksit ödemesi yapar. Bir ödeme yalnızca tek bir takside ait olabilir.
    /// Aynı taksit iki kere ödenemez.
    [HttpPost]
    public async Task<IActionResult> Pay([FromBody] PaymentRequestDto request)
    {
        try
        {
            var result = await _paymentService.PayInstallmentAsync(request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// Tüm ödeme kayıtlarını listeler.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var payments = await _paymentService.GetAllPaymentsAsync();
        return Ok(payments);
    }

    /// Müşteriye ait ödeme kayıtlarını listeler.
    [HttpGet("by-customer/{customerId}")]
    public async Task<IActionResult> GetByCustomer(int customerId)
    {
        var payments = await _paymentService.GetPaymentsByCustomerIdAsync(customerId);
        return Ok(payments);
    }
}
