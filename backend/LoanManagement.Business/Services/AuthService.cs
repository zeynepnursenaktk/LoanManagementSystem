using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using LoanManagement.Business.Abstract;
using LoanManagement.Business.Security;
using LoanManagement.DataAccess.Context;
using LoanManagement.Entities;
using LoanManagement.Entities.DTOs;
using LoanManagement.Entities.Models;
using LoanManagement.Entities.Validation;

namespace LoanManagement.Business.Services;

public class AuthService : IAuthService
{
    private readonly LoanDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(LoanDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<LoginResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        // Defensive validation: controller bypass edilse bile servisi koruruz.
        // Validatorler ham (kullanıcının yazdığı) değer ile çalışır; bu sayede "abc",
        // "(...)" gibi normalize sonrası boş kalacak girdiler de yakalanır.
        if (!TurkishIdentityNumberValidator.IsValid(request.IdentityNumber))
            throw new ArgumentException("Geçersiz T.C. Kimlik Numarası.", nameof(request.IdentityNumber));

        if (!TurkishPhoneNumberValidator.IsValid(request.PhoneNumber))
            throw new ArgumentException("Geçersiz telefon numarası formatı.", nameof(request.PhoneNumber));

        // Normalize girişler (depolama için temizlenmiş hâl).
        var username = (request.Username ?? string.Empty).Trim();
        var firstName = (request.FirstName ?? string.Empty).Trim();
        var lastName = (request.LastName ?? string.Empty).Trim();
        var identityNumber = (request.IdentityNumber ?? string.Empty).Trim();
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var phoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? null
            : TurkishPhoneNumberValidator.Normalize(request.PhoneNumber!);

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Zorunlu alanlar boş bırakılamaz.");

        //gerekli benzersizlik kontrolleri
        var usernameExists = await _context.Users.AnyAsync(u => u.Username == username);
        if (usernameExists)
            throw new InvalidOperationException("Bu kullanıcı adı zaten kullanılmaktadır.");

        var identityExists = await _context.Customers.AnyAsync(c => c.IdentityNumber == identityNumber);
        if (identityExists)
            throw new InvalidOperationException("Bu TC kimlik numarası ile kayıtlı müşteri zaten mevcut.");

        var emailExists = await _context.Customers.AnyAsync(c => c.Email == email);
        if (emailExists)
            throw new InvalidOperationException("Bu e-posta adresi zaten kullanılmaktadır.");

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var customer = new Customer
            {
                FirstName = firstName,
                LastName = lastName,
                IdentityNumber = identityNumber,
                Email = email,
                PhoneNumber = phoneNumber ?? ""
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            var fullName = $"{firstName} {lastName}".Trim();
            var user = new User
            {
                Username = username,
                PasswordHash = PasswordHasher.Hash(request.Password),
                FullName = string.IsNullOrWhiteSpace(fullName) ? username : fullName,
                Role = AppRoles.Customer,
                CustomerId = customer.Id,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await tx.CommitAsync();
            return GenerateToken(user);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Geçersiz kullanıcı adı veya şifre.");

        return GenerateToken(user);
    }

    private LoginResponseDto GenerateToken(User user)
    {
        var role = NormalizeRole(user.Role);
        var jwtKey = _configuration["Jwt:Key"] ?? "LoanManagementSuperSecretKey2026!@#$%^&*()";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.GivenName, user.FullName),
            new(ClaimTypes.Role, role)
        };

        if (user.CustomerId is { } cid)
            claims.Add(new Claim(JwtClaimNames.CustomerId, cid.ToString()));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "LoanManagementAPI",
            audience: _configuration["Jwt:Audience"] ?? "LoanManagementClient",
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return new LoginResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Username = user.Username,
            FullName = user.FullName,
            Role = role,
            CustomerId = user.CustomerId,
            ExpiresAt = expiresAt
        };
    }

    private static string NormalizeRole(string? role) => role switch
    {
        AppRoles.Admin => AppRoles.Admin,
        AppRoles.Customer => AppRoles.Customer,
        "User" => AppRoles.Customer,
        _ => AppRoles.Customer
    };
}
