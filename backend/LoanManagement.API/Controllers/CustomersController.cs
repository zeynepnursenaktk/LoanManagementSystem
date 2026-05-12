using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LoanManagement.Business.Abstract;
using LoanManagement.Entities.DTOs;
using System.ComponentModel.DataAnnotations;
using LoanManagement.API.Security;
using LoanManagement.Entities;

namespace LoanManagement.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ICurrentUserAccessor _current;

    public CustomersController(ICustomerService customerService, ICurrentUserAccessor current)
    {
        _customerService = customerService;
        _current = current;
    }


    private IActionResult? ForbidUnlessAdminOrOwnCustomer(int requestedCustomerId)
    {
        if (_current.IsAdmin) return null;
        if (_current.CustomerId is not int cid)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Bu işlem için müşteri hesabı gereklidir." });
        if (cid != requestedCustomerId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Bu kaynağa erişim yetkiniz yok." });
        return null;
    }


    /// Tüm müşterileri listeler (yalnızca Admin).
    /// <param name="includeDeleted">
    /// <c>true</c> ise soft-delete edilmiş müşteriler de listeye dahil edilir.
    /// Admin paneli durum kolonu ve "Aktife Çevir" akışı için bu parametreyi <c>true</c> gönderir.
    /// Geri uyumluluk için varsayılan <c>false</c>.
    /// </param>
    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> GetCustomers([FromQuery] bool includeDeleted = false)
    {
        var customers = await _customerService.GetCustomersAsync(includeDeleted);
        return Ok(customers);
    }

    /// ID'ye göre müşteri detayını getirir.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCustomerById(int id)
    {
        if (id <= 0)
            return BadRequest(new { message = "Geçersiz müşteri ID. ID 0'dan büyük olmalıdır." });

        var forbid = ForbidUnlessAdminOrOwnCustomer(id);
        if (forbid != null) return forbid;

        var customer = await _customerService.GetCustomerByIdAsync(id);
        if (customer == null)
            return NotFound(new { message = "Müşteri bulunamadı." });

        return Ok(customer);
    }

    /// Yeni müşteri oluşturur (yalnızca Admin). Kayıtlı müşteriler /api/Auth/register ile oluşturulur.
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(new
            {
                message = "Giriş verisi validasyonu başarısız.",
                errors = errors
            });
        }

        try
        {
            var id = await _customerService.CreateCustomerAsync(dto);
            return CreatedAtAction(nameof(GetCustomerById), new { id },
                new { id, message = "Müşteri başarıyla oluşturuldu." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Müşteri oluşturulurken hata oluştu.", details = ex.Message });
        }
    }

    /// Müşteri bilgilerini günceller.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] UpdateCustomerDto dto)
    {
        if (id <= 0)
            return BadRequest(new { message = "Geçersiz müşteri ID. ID 0'dan büyük olmalıdır." });

        var forbid = ForbidUnlessAdminOrOwnCustomer(id);
        if (forbid != null) return forbid;

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
            var ok = await _customerService.UpdateCustomerAsync(id, dto);
            if (!ok) return NotFound(new { message = "Müşteri bulunamadı." });
            return Ok(new { message = "Müşteri başarıyla güncellendi." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Müşteri güncellenirken hata oluştu.", details = ex.Message });
        }
    }

    /// Müşteriyi soft delete ile arşivler (satır kalır; listelerde görünmez). Aktif kredisi olan müşteri silinemez (yalnızca Admin).
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        if (id <= 0)
            return BadRequest(new { message = "Geçersiz müşteri ID. ID 0'dan büyük olmalıdır." });

        try
        {
            var ok = await _customerService.DeleteCustomerAsync(id);
            if (!ok)
                return NotFound(new { message = "Müşteri bulunamadı." });

            return Ok(new { message = "Müşteri silindi (kayıt arşivlendi)." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Müşteri silinirken hata oluştu.", details = ex.Message });
        }
    }

    /// Soft-delete edilmiş müşteriyi yeniden aktifleştirir (yalnızca Admin).
    /// 200: restore başarılı. 404: müşteri bulunamadı veya zaten aktif.
    [HttpPost("{id:int}/restore")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> RestoreCustomer(int id)
    {
        if (id <= 0)
            return BadRequest(new { message = "Geçersiz müşteri ID. ID 0'dan büyük olmalıdır." });

        try
        {
            var ok = await _customerService.RestoreCustomerAsync(id);
            if (!ok)
                return NotFound(new { message = "Müşteri bulunamadı veya zaten aktif." });

            return Ok(new { message = "Müşteri yeniden aktifleştirildi." });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Müşteri aktifleştirilirken hata oluştu.", details = ex.Message });
        }
    }

    /// Müşterinin borç ve kredi özetini getirir.
    [HttpGet("{id:int}/summary")]
    public async Task<IActionResult> GetCustomerSummary(
        [Range(1, int.MaxValue, ErrorMessage = "Müşteri ID 0'dan büyük olmalıdır.")] int id)
    {
        if (id <= 0)
            return BadRequest(new { message = "Geçersiz müşteri ID. ID 0'dan büyük olmalıdır." });

        var forbid = ForbidUnlessAdminOrOwnCustomer(id);
        if (forbid != null) return forbid;

        try
        {
            var summary = await _customerService.GetCustomerSummaryAsync(id);
            if (summary == null)
                return NotFound(new { message = "Müşteri bulunamadı." });

            return Ok(summary);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Özet getirilirken hata oluştu.", details = ex.Message });
        }
    }
}
