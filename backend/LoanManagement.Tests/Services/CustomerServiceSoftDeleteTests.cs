using FluentAssertions;
using LoanManagement.Business.Services;
using LoanManagement.DataAccess.Context;
using LoanManagement.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LoanManagement.Tests.Services;

/// <summary>
/// CustomerService.GetCustomersAsync(includeDeleted) ve RestoreCustomerAsync için
/// in-memory DB ile uçtan uca senaryolar.
///
/// <para>
/// LoanDbContext'te <c>HasQueryFilter(c =&gt; !c.IsDeleted)</c> var; bu nedenle
/// listeleme metodu default olarak silinmişleri görmez. Bu testler hem default
/// davranışı hem de <c>IgnoreQueryFilters()</c> bypass yolunu doğrular.
/// </para>
/// </summary>
public class CustomerServiceSoftDeleteTests : IDisposable
{
    private readonly LoanDbContext _context;

    public CustomerServiceSoftDeleteTests()
    {
        var options = new DbContextOptionsBuilder<LoanDbContext>()
            .UseInMemoryDatabase($"customer-softdelete-{Guid.NewGuid()}")
            .Options;
        _context = new LoanDbContext(options);
    }

    public void Dispose() => _context.Dispose();

    private CustomerService CreateSut() => new(_context);

    private async Task<(Customer active, Customer deleted)> SeedActiveAndDeletedAsync()
    {
        var active = new Customer
        {
            FirstName = "Aktif",
            LastName = "Kullanıcı",
            IdentityNumber = "12345678950",
            Email = "aktif@example.com",
            PhoneNumber = "05551112233",
            IsDeleted = false
        };
        var deleted = new Customer
        {
            FirstName = "Silinmiş",
            LastName = "Kullanıcı",
            IdentityNumber = "11111111110",
            Email = "silinmis@example.com",
            PhoneNumber = "05551112244",
            IsDeleted = true,
            DeletedAtUtc = DateTime.UtcNow.AddDays(-2)
        };

        _context.Customers.AddRange(active, deleted);
        await _context.SaveChangesAsync();
        return (active, deleted);
    }

    [Fact]
    public async Task GetCustomersAsync_DefaultBehavior_ReturnsOnlyActive()
    {
        await SeedActiveAndDeletedAsync();
        var sut = CreateSut();

        var result = await sut.GetCustomersAsync();

        result.Should().HaveCount(1);
        result[0].FirstName.Should().Be("Aktif");
        result[0].IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetCustomersAsync_WithIncludeDeleted_ReturnsAllIncludingDeleted()
    {
        var (_, deleted) = await SeedActiveAndDeletedAsync();
        var sut = CreateSut();

        var result = await sut.GetCustomersAsync(includeDeleted: true);

        result.Should().HaveCount(2);
        result.Should().Contain(c => c.IsDeleted && c.Email == deleted.Email);
        result.Should().Contain(c => !c.IsDeleted);
        var del = result.Single(c => c.IsDeleted);
        del.DeletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task RestoreCustomerAsync_WithDeletedCustomer_ResetsFlagsAndReturnsTrue()
    {
        var (_, deleted) = await SeedActiveAndDeletedAsync();
        var sut = CreateSut();

        var ok = await sut.RestoreCustomerAsync(deleted.Id);

        ok.Should().BeTrue();

        // Filter bypass ile tekrar oku, durum aktif olmalı
        var fromDb = await _context.Customers
            .IgnoreQueryFilters()
            .FirstAsync(c => c.Id == deleted.Id);
        fromDb.IsDeleted.Should().BeFalse();
        fromDb.DeletedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task RestoreCustomerAsync_WithAlreadyActiveCustomer_ReturnsFalse()
    {
        var (active, _) = await SeedActiveAndDeletedAsync();
        var sut = CreateSut();

        var ok = await sut.RestoreCustomerAsync(active.Id);

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task RestoreCustomerAsync_WithNonExistentId_ReturnsFalse()
    {
        var sut = CreateSut();

        var ok = await sut.RestoreCustomerAsync(999_999);

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteThenRestore_RoundTrip_LeavesCustomerActive()
    {
        var (active, _) = await SeedActiveAndDeletedAsync();
        var sut = CreateSut();

        var deleted = await sut.DeleteCustomerAsync(active.Id);
        deleted.Should().BeTrue();

        var deletedRow = await _context.Customers
            .IgnoreQueryFilters()
            .FirstAsync(c => c.Id == active.Id);
        deletedRow.IsDeleted.Should().BeTrue();
        deletedRow.DeletedAtUtc.Should().NotBeNull();

        var restored = await sut.RestoreCustomerAsync(active.Id);
        restored.Should().BeTrue();

        var finalRow = await _context.Customers
            .IgnoreQueryFilters()
            .FirstAsync(c => c.Id == active.Id);
        finalRow.IsDeleted.Should().BeFalse();
        finalRow.DeletedAtUtc.Should().BeNull();
    }
}
