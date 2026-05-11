using Microsoft.AspNetCore.Mvc;
using LoanManagement.Business.Abstract;
using LoanManagement.Entities.Models;

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

    [HttpPost]
    public async Task<IActionResult> CreateLoan(Loan loan)
    {
        // Önemli: Kredi çekilirken başlangıç tarihi bugün set ediliyor
        loan.StartDate = DateTime.Now;

        await _loanService.CreateLoanWithInstallmentsAsync(loan);
        return Ok(loan);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        // Artık DTO dönen metodu çağırıyoruz
        var loanDto = await _loanService.GetLoanByIdDtoAsync(id);

        if (loanDto == null)
            return NotFound("Kredi bulunamadı.");

        return Ok(loanDto);
    }
}