using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using LoanManagement.API.Controllers;
using LoanManagement.Business.Abstract;
using LoanManagement.Entities.DTOs;
using LoanManagement.Tests.Fixtures;
using LoanManagement.Tests.TestData;

namespace LoanManagement.Tests.Controllers;

[Collection("LoansController Collection")]
public class LoansControllerTests
{
    private readonly LoansControllerFixture _fixture;
    private readonly LoansController _controller;
    private readonly Mock<ILoanService> _mockLoanService;

    public LoansControllerTests(LoansControllerFixture fixture)
    {
        _fixture = fixture;
        _controller = fixture.Controller;
        _mockLoanService = fixture.MockLoanService;
        _fixture.ResetMocks();
    }

    #region GetLoans

    [Fact]
    public async Task GetLoans_WithLoans_ReturnsOkWithList()
    {
        var loans = new List<LoanResponseDto>
        {
            LoanTestData.ValidLoanResponseDto(1, 1),
            LoanTestData.ValidLoanResponseDto(2, 2)
        };
        _mockLoanService.Setup(s => s.GetLoansAsync()).ReturnsAsync(loans);

        var result = await _controller.GetLoans();

        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(loans);
        _mockLoanService.Verify(s => s.GetLoansAsync(), Times.Once);
    }

    [Fact]
    public async Task GetLoans_WithEmptyList_ReturnsOk()
    {
        _mockLoanService.Setup(s => s.GetLoansAsync()).ReturnsAsync(new List<LoanResponseDto>());

        var result = await _controller.GetLoans();

        result.Should().BeOfType<OkObjectResult>();
        var list = ((OkObjectResult)result).Value as List<LoanResponseDto>;
        list.Should().NotBeNull();
        list!.Count.Should().Be(0);
    }

    [Fact]
    public void GetLoans_ControllerHasAuthorizeAttribute()
    {
        var attrs = typeof(LoansController).GetCustomAttributes(
            typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false);
        attrs.Length.Should().BeGreaterThan(0);
    }

    #endregion

    #region CreateLoan

    [Fact]
    public async Task CreateLoan_WithValidDto_ReturnsCreatedAtActionWithLoan()
    {
        var dto = LoanTestData.ValidLoanRequestDto();
        var newId = 42;
        var loanDto = LoanTestData.ValidLoanResponseDto(newId, dto.CustomerId);
        _mockLoanService.Setup(s => s.CreateLoanAsync(It.IsAny<LoanRequestDto>())).ReturnsAsync(newId);
        _mockLoanService.Setup(s => s.GetLoanByIdAsync(newId)).ReturnsAsync(loanDto);
        _controller.ModelState.Clear();

        var result = await _controller.CreateLoan(dto);

        result.Should().BeOfType<CreatedAtActionResult>();
        var created = (CreatedAtActionResult)result;
        created.StatusCode.Should().Be(201);
        created.ActionName.Should().Be(nameof(LoansController.GetLoanById));
        created.RouteValues!["id"].Should().Be(newId);
        created.Value.Should().Be(loanDto);
        _mockLoanService.Verify(s => s.CreateLoanAsync(It.IsAny<LoanRequestDto>()), Times.Once);
        _mockLoanService.Verify(s => s.GetLoanByIdAsync(newId), Times.Once);
    }

    [Fact]
    public async Task CreateLoan_WithInvalidModelState_ReturnsBadRequestWithErrors()
    {
        var dto = LoanTestData.ValidLoanRequestDto();
        _controller.ModelState.AddModelError("Amount", "Kredi tutarı geçersiz.");

        var result = await _controller.CreateLoan(dto);

        result.Should().BeOfType<BadRequestObjectResult>();
        var bad = (BadRequestObjectResult)result;
        bad.StatusCode.Should().Be(400);
        _mockLoanService.Verify(s => s.CreateLoanAsync(It.IsAny<LoanRequestDto>()), Times.Never);
    }

    [Fact]
    public async Task CreateLoan_WhenCustomerNotFound_ReturnsNotFound()
    {
        var dto = LoanTestData.ValidLoanRequestDto();
        _mockLoanService.Setup(s => s.CreateLoanAsync(It.IsAny<LoanRequestDto>()))
            .ThrowsAsync(new KeyNotFoundException("Müşteri bulunamadı."));
        _controller.ModelState.Clear();

        var result = await _controller.CreateLoan(dto);

        result.Should().BeOfType<NotFoundObjectResult>();
        ((NotFoundObjectResult)result).StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateLoan_WhenCreditScoreInsufficient_ReturnsBadRequest()
    {
        var dto = LoanTestData.ValidLoanRequestDto();
        _mockLoanService.Setup(s => s.CreateLoanAsync(It.IsAny<LoanRequestDto>()))
            .ThrowsAsync(new InvalidOperationException("Skor yetersiz."));
        _controller.ModelState.Clear();

        var result = await _controller.CreateLoan(dto);

        result.Should().BeOfType<BadRequestObjectResult>();
        ((BadRequestObjectResult)result).StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateLoan_WhenArgumentException_ReturnsBadRequest()
    {
        var dto = LoanTestData.ValidLoanRequestDto();
        _mockLoanService.Setup(s => s.CreateLoanAsync(It.IsAny<LoanRequestDto>()))
            .ThrowsAsync(new ArgumentException("Geçersiz parametre."));
        _controller.ModelState.Clear();

        var result = await _controller.CreateLoan(dto);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetLoanById

    [Fact]
    public async Task GetLoanById_WithValidId_ReturnsOk()
    {
        var loan = LoanTestData.ValidLoanResponseDto(5, 1);
        _mockLoanService.Setup(s => s.GetLoanByIdAsync(5)).ReturnsAsync(loan);

        var result = await _controller.GetLoanById(5);

        result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)result).Value.Should().Be(loan);
        _mockLoanService.Verify(s => s.GetLoanByIdAsync(5), Times.Once);
    }

    [Fact]
    public async Task GetLoanById_WhenLoanMissing_ReturnsNotFound()
    {
        _mockLoanService.Setup(s => s.GetLoanByIdAsync(99)).ReturnsAsync((LoanResponseDto?)null);

        var result = await _controller.GetLoanById(99);

        result.Should().BeOfType<NotFoundObjectResult>();
        ((NotFoundObjectResult)result).StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetLoanById_WithZeroId_ReturnsBadRequest()
    {
        var result = await _controller.GetLoanById(0);

        result.Should().BeOfType<BadRequestObjectResult>();
        _mockLoanService.Verify(s => s.GetLoanByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetLoanById_WithNegativeId_ReturnsBadRequest()
    {
        var result = await _controller.GetLoanById(-3);

        result.Should().BeOfType<BadRequestObjectResult>();
        _mockLoanService.Verify(s => s.GetLoanByIdAsync(It.IsAny<int>()), Times.Never);
    }

    #endregion

    #region GetLoansByCustomerId

    [Fact]
    public async Task GetLoansByCustomerId_WithValidId_ReturnsOk()
    {
        var loans = new List<LoanResponseDto> { LoanTestData.ValidLoanResponseDto(1, 7) };
        _mockLoanService.Setup(s => s.GetLoansByCustomerIdAsync(7)).ReturnsAsync(loans);

        var result = await _controller.GetLoansByCustomerId(7);

        result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)result).Value.Should().Be(loans);
        _mockLoanService.Verify(s => s.GetLoansByCustomerIdAsync(7), Times.Once);
    }

    [Fact]
    public async Task GetLoansByCustomerId_WithZeroId_ReturnsBadRequest()
    {
        var result = await _controller.GetLoansByCustomerId(0);

        result.Should().BeOfType<BadRequestObjectResult>();
        _mockLoanService.Verify(s => s.GetLoansByCustomerIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetLoansByCustomerId_WithNegativeId_ReturnsBadRequest()
    {
        var result = await _controller.GetLoansByCustomerId(-1);

        result.Should().BeOfType<BadRequestObjectResult>();
        _mockLoanService.Verify(s => s.GetLoansByCustomerIdAsync(It.IsAny<int>()), Times.Never);
    }

    #endregion
}
