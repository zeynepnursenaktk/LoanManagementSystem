
using LoanManagement.DataAccess.Context;
using Microsoft.EntityFrameworkCore;

using LoanManagement.Business.Abstract;
using LoanManagement.Business.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<LoanDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddControllers();

// Loan servisi
builder.Services.AddScoped<ILoanService, LoanService>();

// Payment servisi
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Mock kredi skoru servisi (Üçüncü parti servis entegrasyonu)
builder.Services.AddScoped<ICreditScoreService, MockCreditScoreService>();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();