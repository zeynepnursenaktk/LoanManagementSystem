using Microsoft.AspNetCore.Mvc;
using LoanManagement.Business.Abstract;
using LoanManagement.Entities.Models;
using LoanManagement.Entities.DTOs;
using LoanManagement.Entities.Enums;

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
    public async Task<IActionResult> CreateLoan([FromBody] CreateLoanDto dto)
    {
        try
        {
            var loan = new Loan
            {
                CustomerId = dto.CustomerId,
                Amount = dto.Amount,
                Tenor = dto.Tenor,
                ProfitRate = dto.ProfitRate,
                LoanType = dto.LoanType,
                StartDate = DateTime.Now
            };

            await _loanService.CreateLoanWithInstallmentsAsync(loan);
            var loanDto = await _loanService.GetLoanByIdDtoAsync(loan.Id);
            return Ok(loanDto);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var loanDto = await _loanService.GetLoanByIdDtoAsync(id);

        if (loanDto == null)
            return NotFound("Kredi bulunamadı.");

        return Ok(loanDto);
    }

    [HttpPost("pay-installment")]
    public async Task<IActionResult> PayInstallment([FromBody] PaymentRequestDto request)
    {
        try
        {
            var result = await _loanService.PayInstallmentAsync(request);

            if (result == null)
                return NotFound("Taksit bulunamadı.");

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var loans = await _loanService.GetAllLoansDtoAsync();
        return Ok(loans);
    }
}