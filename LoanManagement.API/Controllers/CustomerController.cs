using Microsoft.AspNetCore.Mvc;
using LoanManagement.Business.Abstract;
using LoanManagement.Entities.DTOs;

[Route("api/[controller]")]
[ApiController]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    // GET: api/customers
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var customers = await _customerService.GetAllAsync();
        return Ok(customers);
    }

    // GET: api/customers/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer == null) return NotFound("Müşteri bulunamadı.");
        return Ok(customer);
    }

    // POST: api/customers
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
    {
        var id = await _customerService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    // PUT: api/customers/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerDto dto)
    {
        var ok = await _customerService.UpdateAsync(id, dto);
        if (!ok) return NotFound("Müşteri bulunamadı.");
        return Ok("Güncellendi.");
    }

    // GET: api/customers/{id}/summary
    [HttpGet("{id}/summary")]
    public async Task<IActionResult> GetSummary(int id)
    {
        var summary = await _customerService.GetCustomerSummaryAsync(id);
        if (summary == null) return NotFound("Müşteri bulunamadı.");
        return Ok(summary);
    }
}