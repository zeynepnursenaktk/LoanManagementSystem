using LoanManagement.Business.Security;
using LoanManagement.DataAccess.Context;
using LoanManagement.Entities;
using LoanManagement.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LoanManagement.Business.Services;

public static class AdminUserSeedService
{
    public static async Task SeedAsync(LoanDbContext context, IConfiguration configuration, ILogger logger)
    {
        if (await context.Users.AnyAsync(u => u.Role == AppRoles.Admin))
            return;

        var username = configuration["Seed:AdminUsername"] ?? "admin";
        var password = configuration["Seed:AdminPassword"] ?? "Admin123!";
        var fullName = configuration["Seed:AdminFullName"] ?? "Sistem Yöneticisi";

        if (await context.Users.AnyAsync(u => u.Username == username))
        {
            logger.LogWarning("Admin tohumlaması atlandı: '{Username}' zaten var.", username);
            return;
        }

        context.Users.Add(new User
        {
            Username = username,
            PasswordHash = PasswordHasher.Hash(password),
            FullName = fullName,
            Role = AppRoles.Admin,
            CustomerId = null,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        logger.LogInformation("İlk Admin kullanıcı oluşturuldu: {Username}", username);
    }
}
