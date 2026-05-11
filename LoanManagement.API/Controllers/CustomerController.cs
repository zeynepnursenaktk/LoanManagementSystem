using Microsoft.AspNetCore.Mvc;
using LoanManagement.Business.Abstract;
using LoanManagement.Entities.Models;
using LoanManagement.Entities.DTOs;


namespace LoanManagement.API.Controllers;

[Route("api/[controller]")]
[ApiController] // Bu attribute, gelen verilerin otomatik doğrulanmasını sağlar
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var customers = await _customerService.GetAllAsync();

        var response = customers.Select(c => new CustomerResponseDto
        {
            Id = c.Id,
            FirstName = c.FirstName,
            LastName = c.LastName,
            Email = c.Email,
            PhoneNumber = c.PhoneNumber,
            IdentityNumber = c.IdentityNumber,
            Loans = (c.Loans ?? new List<Loan>()).Select(l => new LoanSummaryDto
            {
                Id = l.Id,
                Amount = l.Amount,
                Tenor = l.Tenor,
                Status = l.Status.ToString(),
                StartDate = l.StartDate
            }).ToList()
        }).ToList();

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer == null) return NotFound("Müşteri bulunamadı.");

        var response = new CustomerResponseDto
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email,
            PhoneNumber = customer.PhoneNumber,
            IdentityNumber = customer.IdentityNumber,
            Loans = (customer.Loans ?? new List<Loan>()).Select(l => new LoanSummaryDto
            {
                Id = l.Id,
                Amount = l.Amount,
                Tenor = l.Tenor,
                Status = l.Status.ToString(),
                StartDate = l.StartDate
            }).ToList()
        };

        return Ok(response);
    }
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
    {
        // DTO'dan gelen verilerle yeni bir Entity (Müşteri) oluşturuyoruz
        var customer = new Customer
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            IdentityNumber = dto.IdentityNumber,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber
        };

        await _customerService.AddAsync(customer);

        // Başarıyla oluşturulduğunu bildirirken sadece ID dönmek yeterlidir
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, new { id = customer.Id });
    }


    [HttpGet("{id}/summary")]
    public async Task<IActionResult> GetSummary(int id)
    {
        var summary = await _customerService.GetCustomerSummaryAsync(id);

        if (summary == null)
            return NotFound("Müşteri bulunamadı.");

        return Ok(summary);
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerDto dto)
    {
        var result = await _customerService.UpdateAsync(id, dto);

        if (!result)
            return NotFound("Müşteri bulunamadı.");

        return Ok("Müşteri bilgileri başarıyla güncellendi.");
    }
}