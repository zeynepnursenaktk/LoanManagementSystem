using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LoanManagement.Business.Abstract;
using LoanManagement.Business.ExternalServices;
using LoanManagement.Entities.DTOs;
using LoanManagement.API.Security;
using LoanManagement.Entities;

namespace LoanManagement.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ICurrentUserAccessor _current;

    public PaymentsController(IPaymentService paymentService, ICurrentUserAccessor current)
    {
        _paymentService = paymentService;
        _current = current;
    }


    [HttpPost]
    public async Task<IActionResult> Pay([FromBody] PaymentRequestDto request)
    {
        try
        {
            int? scoped = _current.IsAdmin ? null : _current.CustomerId;
            if (!_current.IsAdmin && scoped is null)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Ödeme için müşteri hesabı gereklidir." });

            var result = await _paymentService.PayInstallmentAsync(request, scoped);
            return Ok(result);
        }
        catch (PaymentDeclinedException ex)
        {
            // 402 Payment Required — Stripe-uyumlu decline yanıtı.
            return StatusCode(StatusCodes.Status402PaymentRequired, new
            {
                status = ex.Status.ToString(),
                declineCode = ex.DeclineCode,
                providerName = ex.ProviderName,
                message = ex.Message,
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// Tüm ödeme kayıtlarını listeler (yalnızca Admin).
    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> GetAll()
    {
        var payments = await _paymentService.GetAllPaymentsAsync();
        return Ok(payments);
    }

    /// Müşteriye ait ödeme kayıtlarını listeler.
    [HttpGet("by-customer/{customerId}")]
    public async Task<IActionResult> GetByCustomer(int customerId)
    {
        if (!_current.IsAdmin)
        {
            if (_current.CustomerId is not int cid || cid != customerId)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Bu müşterinin ödemelerini görüntüleme yetkiniz yok." });
        }

        var payments = await _paymentService.GetPaymentsByCustomerIdAsync(customerId);
        return Ok(payments);
    }
}
