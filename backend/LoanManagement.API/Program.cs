using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LoanManagement.DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using LoanManagement.Business.Abstract;
using LoanManagement.Business.ExternalServices;
using LoanManagement.Business.Services;
using LoanManagement.API.Security;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using LoanManagement.API.Middleware;
using Polly;
using Polly.Extensions.Http;

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();

const string FrontendCorsPolicy = "FrontendCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        var configuredOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>();

        var origins = (configuredOrigins is { Length: > 0 })
            ? configuredOrigins
            : new[]
            {
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:4173",
                "http://localhost:3000"
            };

        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<LoanDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IInstallmentService, InstallmentService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.Configure<ExternalServicesOptions>(
    builder.Configuration.GetSection(ExternalServicesOptions.SectionName));

builder.Services.AddScoped<ICreditScoreService, ExternalCreditScoreService>();
builder.Services.AddScoped<IExternalCreditScoreService, ExternalCreditScoreService>();
builder.Services.AddScoped<IMockPaymentGatewayService, ExternalPaymentGatewayService>();
builder.Services.AddScoped<IExternalPaymentGatewayService, ExternalPaymentGatewayService>();

// ---- Dış servis HttpClient'ları (HttpClientFactory + Polly retry + opsiyonel sandbox handler) ----
var externalOptions = builder.Configuration
    .GetSection(ExternalServicesOptions.SectionName)
    .Get<ExternalServicesOptions>() ?? new ExternalServicesOptions();

if (externalOptions.PaymentGateway.UseSandboxHandler)
    builder.Services.AddTransient<StripeSandboxDelegatingHandler>();

if (externalOptions.CreditBureau.UseSandboxHandler)
    builder.Services.AddTransient<CreditBureauSandboxDelegatingHandler>();

var paymentHttpBuilder = builder.Services.AddHttpClient<IExternalPaymentGatewayClient, StripeMockPaymentGatewayClient>(c =>
{
    c.BaseAddress = new Uri(externalOptions.PaymentGateway.BaseUrl);
    c.Timeout = TimeSpan.FromSeconds(externalOptions.PaymentGateway.TimeoutSeconds);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("LoanManagement/1.1 (+sandbox)");
});

if (externalOptions.PaymentGateway.UseSandboxHandler)
    paymentHttpBuilder.AddHttpMessageHandler<StripeSandboxDelegatingHandler>();

paymentHttpBuilder.AddPolicyHandler(GetRetryPolicy(externalOptions.PaymentGateway.RetryCount));

var bureauHttpBuilder = builder.Services.AddHttpClient<IExternalCreditBureauClient, MockCreditBureauClient>(c =>
{
    c.BaseAddress = new Uri(externalOptions.CreditBureau.BaseUrl);
    c.Timeout = TimeSpan.FromSeconds(externalOptions.CreditBureau.TimeoutSeconds);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("LoanManagement/1.1 (+bureau)");
});

if (externalOptions.CreditBureau.UseSandboxHandler)
    bureauHttpBuilder.AddHttpMessageHandler<CreditBureauSandboxDelegatingHandler>();

bureauHttpBuilder.AddPolicyHandler(GetRetryPolicy(externalOptions.CreditBureau.RetryCount));

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount) =>
    HttpPolicyExtensions
        .HandleTransientHttpError() // 5xx, 408
        .WaitAndRetryAsync(
            retryCount,
            attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1)));

var jwtKey = builder.Configuration["Jwt:Key"] ?? "LoanManagementSuperSecretKey2026!@#$%^&*()";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "LoanManagementAPI",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "LoanManagementClient",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LoanManagement API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Bearer {token} formatında girin."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LoanDbContext>();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var seedLogger = loggerFactory.CreateLogger("AdminUserSeed");
    await AdminUserSeedService.SeedAsync(db, app.Configuration, seedLogger);
}

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
