using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LoanManagement.Business.Abstract;
using LoanManagement.API.Security;

namespace LoanManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CreditScoresController : ControllerBase
{
    private readonly ICreditScoreService _creditScoreService;
    private readonly IExternalCreditScoreService _externalCreditScoreService;
    private readonly ICurrentUserAccessor _current;

    public CreditScoresController(
        ICreditScoreService creditScoreService,
        IExternalCreditScoreService externalCreditScoreService,
        ICurrentUserAccessor current)
    {
        _creditScoreService = creditScoreService;
        _externalCreditScoreService = externalCreditScoreService;
        _current = current;
    }

    /// Müşterinin kredi skorunu dış bürodan (Findeks Mock) sorgular.
    /// Yanıt artık skor + risk seviyesi + sağlayıcı + sorgu zamanı + referans içerir.
    [HttpGet("{customerId}")]
    public async Task<IActionResult> GetScore(int customerId)
    {
        if (customerId <= 0)
            return BadRequest(new { message = "Geçersiz müşteri ID. ID 0'dan büyük olmalıdır." });

        if (!_current.IsAdmin)
        {
            if (_current.CustomerId is not int cid || cid != customerId)
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { message = "Yalnızca kendi kredi skorunuzu sorgulayabilirsiniz." });
        }

        try
        {
            var detailed = await _externalCreditScoreService.GetDetailedScoreAsync(customerId);
            return Ok(new
            {
                customerId = detailed.CustomerId,
                creditScore = detailed.Score,
                riskLevel = detailed.RiskLevel,
                providerName = detailed.ProviderName,
                providerReference = detailed.ProviderReference,
                queriedAtUtc = detailed.QueriedAtUtc,
                isEligible = detailed.Score >= 600,
                factors = detailed.Factors.Select(f => new { code = f.Code, label = f.Label, delta = f.Delta }),
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Dış sağlayıcıya ulaşılamadı → 503 Service Unavailable.
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                providerError = true,
                message = ex.Message,
            });
        }
    }

    /// Lightweight legacy endpoint (numeric-only) — geriye dönük uyumluluk için bırakıldı.
    [HttpGet("{customerId}/score")]
    public async Task<IActionResult> GetScoreNumeric(int customerId)
    {
        try
        {
            var score = await _creditScoreService.GetCreditScoreAsync(customerId);
            return Ok(new { customerId, creditScore = score });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
