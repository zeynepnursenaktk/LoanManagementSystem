using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LoanManagement.Business.Abstract;

namespace LoanManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CreditScoresController : ControllerBase
{
    private readonly ICreditScoreService _creditScoreService;

    public CreditScoresController(ICreditScoreService creditScoreService)
    {
        _creditScoreService = creditScoreService;
    }

    /// Müşterinin kredi skorunu sorgular (Mock servis).
    [HttpGet("{customerId}")]
    public async Task<IActionResult> GetScore(int customerId)
    {
        try
        {
            int score = await _creditScoreService.GetCreditScoreAsync(customerId);
            return Ok(new { customerId, creditScore = score });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
