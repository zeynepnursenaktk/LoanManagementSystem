using FluentAssertions;
using LoanManagement.Business.Abstract;
using LoanManagement.Entities.DTOs;
using LoanManagement.Tests.Fixtures;
using LoanManagement.Tests.TestData;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace LoanManagement.Tests.Controllers;

/// <summary>
/// CustomersController için "soft-delete sonrası listeleme + restore" akış testleri.
/// </summary>
[Collection("CustomersController Collection")]
public class CustomersControllerRestoreTests
{
    private readonly CustomersControllerFixture _fixture;
    private readonly Mock<ICustomerService> _mockService;

    public CustomersControllerRestoreTests(CustomersControllerFixture fixture)
    {
        _fixture = fixture;
        _mockService = fixture.MockCustomerService;
        _fixture.ResetMocks();
    }

    [Fact]
    public async Task GetCustomers_DefaultsToIncludeDeletedFalse()
    {
        _mockService.Setup(s => s.GetCustomersAsync(false))
            .ReturnsAsync(new List<CustomerListDto>());

        var result = await _fixture.Controller.GetCustomers();

        result.Should().BeOfType<OkObjectResult>();
        _mockService.Verify(s => s.GetCustomersAsync(false), Times.Once);
        _mockService.Verify(s => s.GetCustomersAsync(true), Times.Never);
    }

    [Fact]
    public async Task GetCustomers_WithIncludeDeletedTrue_PassesParameterToService()
    {
        var deleted = CustomerTestData.GetValidCustomerListDto(2);
        deleted.IsDeleted = true;
        deleted.DeletedAtUtc = DateTime.UtcNow.AddDays(-1);

        _mockService.Setup(s => s.GetCustomersAsync(true))
            .ReturnsAsync(new List<CustomerListDto>
            {
                CustomerTestData.GetValidCustomerListDto(1),
                deleted
            });

        var result = await _fixture.Controller.GetCustomers(includeDeleted: true);

        result.Should().BeOfType<OkObjectResult>();
        var ok = result as OkObjectResult;
        var list = ok!.Value as List<CustomerListDto>;
        list.Should().HaveCount(2);
        list!.Should().Contain(c => c.IsDeleted);
        _mockService.Verify(s => s.GetCustomersAsync(true), Times.Once);
    }

    [Fact]
    public async Task RestoreCustomer_WithValidId_ReturnsOk()
    {
        const int customerId = 5;
        _mockService.Setup(s => s.RestoreCustomerAsync(customerId)).ReturnsAsync(true);

        var result = await _fixture.Controller.RestoreCustomer(customerId);

        result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)result).StatusCode.Should().Be(200);
        _mockService.Verify(s => s.RestoreCustomerAsync(customerId), Times.Once);
    }

    [Fact]
    public async Task RestoreCustomer_WhenNotFoundOrAlreadyActive_ReturnsNotFound()
    {
        const int customerId = 5;
        _mockService.Setup(s => s.RestoreCustomerAsync(customerId)).ReturnsAsync(false);

        var result = await _fixture.Controller.RestoreCustomer(customerId);

        result.Should().BeOfType<NotFoundObjectResult>();
        ((NotFoundObjectResult)result).StatusCode.Should().Be(404);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task RestoreCustomer_WithInvalidId_ReturnsBadRequest(int id)
    {
        var result = await _fixture.Controller.RestoreCustomer(id);

        result.Should().BeOfType<BadRequestObjectResult>();
        ((BadRequestObjectResult)result).StatusCode.Should().Be(400);
        _mockService.Verify(s => s.RestoreCustomerAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RestoreCustomer_WhenServiceThrows_ReturnsInternalServerError()
    {
        const int customerId = 5;
        _mockService.Setup(s => s.RestoreCustomerAsync(customerId))
            .ThrowsAsync(new Exception("DB down"));

        var result = await _fixture.Controller.RestoreCustomer(customerId);

        result.Should().BeOfType<ObjectResult>();
        ((ObjectResult)result).StatusCode.Should().Be(500);
    }
}
