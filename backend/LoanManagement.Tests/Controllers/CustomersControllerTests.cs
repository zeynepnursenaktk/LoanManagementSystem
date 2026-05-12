using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using LoanManagement.API.Controllers;
using LoanManagement.Business.Abstract;
using LoanManagement.Entities.DTOs;
using LoanManagement.Tests.Fixtures;
using LoanManagement.Tests.TestData;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LoanManagement.Tests.Controllers;

/// <summary>
/// CustomersController Unit Tests - %100 Code Coverage
/// 
/// TEST COVERAGE:
/// - GetCustomers: 3 test case'i
/// - GetCustomerById: 4 test case'i
/// - CreateCustomer: 11 test case'i
/// - UpdateCustomer: 6 test case'i
/// - DeleteCustomer: 6 test case'i
/// - GetCustomerSummary: 4 test case'i
/// 
/// Toplam: 34 test case'i
/// </summary>
[Collection("CustomersController Collection")]
public class CustomersControllerTests
{
    private readonly CustomersControllerFixture _fixture;
    private readonly CustomersController _controller;
    private readonly Mock<ICustomerService> _mockService;

    public CustomersControllerTests(CustomersControllerFixture fixture)
    {
        _fixture = fixture;
        _controller = fixture.Controller;
        _mockService = fixture.MockCustomerService;
        _fixture.ResetMocks();
    }

    #region GetCustomers Tests

    /// <summary>
    /// Test Case 1: GetAll - Başarılı (Liste döner)
    /// Scenario: Service'ten müşteri listesi döner
    /// Expected: 200 OK + müşteri listesi
    /// </summary>
    [Fact]
    public async Task GetAll_WithValidRequest_ReturnsOkWithCustomerList()
    {
        // Arrange
        var customers = new List<CustomerListDto>
        {
            CustomerTestData.GetValidCustomerListDto(1),
            CustomerTestData.GetValidCustomerListDto(2),
            CustomerTestData.GetValidCustomerListDto(3)
        };
        _mockService.Setup(s => s.GetCustomersAsync(It.IsAny<bool>())).ReturnsAsync(customers);

        // Act
        var result = await _controller.GetCustomers();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<OkObjectResult>();
        
        var okResult = result as OkObjectResult;
        okResult?.StatusCode.Should().Be(200);
        
        var returnedCustomers = okResult?.Value as List<CustomerListDto>;
        returnedCustomers.Should().NotBeNull();
        returnedCustomers?.Count.Should().Be(3);
        returnedCustomers?[0].Id.Should().Be(1);
        
        _mockService.Verify(s => s.GetCustomersAsync(It.IsAny<bool>()), Times.Once);
    }

    /// <summary>
    /// Test Case 2: GetAll - Boş Sonuç (Müşteri yok)
    /// Scenario: Service'ten boş liste döner
    /// Expected: 200 OK + boş liste
    /// </summary>
    [Fact]
    public async Task GetAll_WithNoCustomers_ReturnsOkWithEmptyList()
    {
        // Arrange
        var customers = new List<CustomerListDto>();
        _mockService.Setup(s => s.GetCustomersAsync(It.IsAny<bool>())).ReturnsAsync(customers);

        // Act
        var result = await _controller.GetCustomers();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var returnedCustomers = okResult?.Value as List<CustomerListDto>;
        returnedCustomers?.Count.Should().Be(0);
    }

    /// <summary>
    /// Test Case 3: GetAll - Authorization Check
    /// Note: [Authorize] attribute Controller'da tanımlı
    /// Controller class level'de [Authorize] attribute var
    /// </summary>
    [Fact]
    public void GetAll_HasAuthorizeAttribute()
    {
        // Arrange & Act
        var controllerType = typeof(CustomersController);
        var authorizeAttributes = controllerType.GetCustomAttributes(
            typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), 
            false);

        // Assert
        authorizeAttributes.Length.Should().BeGreaterThan(0);
        authorizeAttributes[0].Should().BeOfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();
    }

    #endregion

    #region GetCustomerById Tests

    /// <summary>
    /// Test Case 4: GetById - Başarılı (Müşteri bulundu)
    /// Scenario: Geçerli ID ile müşteri bulunur
    /// Expected: 200 OK + müşteri detayları
    /// </summary>
    [Fact]
    public async Task GetById_WithValidId_ReturnsOkWithCustomer()
    {
        // Arrange
        var customerId = 1;
        var customer = CustomerTestData.GetValidCustomerResponseDto(customerId);
        _mockService.Setup(s => s.GetCustomerByIdAsync(customerId)).ReturnsAsync(customer);

        // Act
        var result = await _controller.GetCustomerById(customerId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<OkObjectResult>();
        
        var okResult = result as OkObjectResult;
        okResult?.StatusCode.Should().Be(200);
        
        var returnedCustomer = okResult?.Value as CustomerResponseDto;
        returnedCustomer?.Id.Should().Be(customerId);
        returnedCustomer?.FirstName.Should().Be("Test");
        
        _mockService.Verify(s => s.GetCustomerByIdAsync(customerId), Times.Once);
    }

    /// <summary>
    /// Test Case 5: GetById - Müşteri Bulunamadı (404)
    /// Scenario: Geçerli ID ama service'te müşteri yok
    /// Expected: 404 Not Found
    /// </summary>
    [Fact]
    public async Task GetById_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var customerId = 999;
        _mockService.Setup(s => s.GetCustomerByIdAsync(customerId)).ReturnsAsync((CustomerResponseDto?)null);

        // Act
        var result = await _controller.GetCustomerById(customerId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult?.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Test Case 6: GetById - Geçersiz ID (Negatif)
    /// Scenario: Negatif ID sağlanır
    /// Expected: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task GetById_WithNegativeId_ReturnsBadRequest()
    {
        // Arrange
        var customerId = -1;

        // Act
        var result = await _controller.GetCustomerById(customerId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        badRequest?.StatusCode.Should().Be(400);
    }

    /// <summary>
    /// Test Case 7: GetById - Geçersiz ID (Sıfır)
    /// Scenario: Sıfır ID sağlanır
    /// Expected: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task GetById_WithZeroId_ReturnsBadRequest()
    {
        // Arrange
        var customerId = 0;

        // Act
        var result = await _controller.GetCustomerById(customerId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region CreateCustomer Tests

    /// <summary>
    /// Test Case 8: Create - Başarılı Oluşturma
    /// Scenario: Geçerli DTO ile müşteri oluşturulur
    /// Expected: 201 Created At Action
    /// </summary>
    [Fact]
    public async Task Create_WithValidDto_ReturnsCreatedAtAction()
    {
        // Arrange
        var dto = CustomerTestData.ValidCreateCustomerDto();
        var newCustomerId = 1;
        _mockService.Setup(s => s.CreateCustomerAsync(It.IsAny<CreateCustomerDto>())).ReturnsAsync(newCustomerId);
        
        // ModelState'i valid yap
        _controller.ModelState.Clear();

        // Act
        var result = await _controller.CreateCustomer(dto);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        createdResult?.StatusCode.Should().Be(201);
        createdResult?.ActionName.Should().Be(nameof(CustomersController.GetCustomerById));
        createdResult?.RouteValues?["id"].Should().Be(newCustomerId);
        
        _mockService.Verify(s => s.CreateCustomerAsync(It.IsAny<CreateCustomerDto>()), Times.Once);
    }

    /// <summary>
    /// Test Case 9: Create - TCNo Zaten Var (409 Conflict)
    /// Scenario: Aynı TCNo ile müşteri zaten var
    /// Expected: 409 Conflict
    /// </summary>
    [Fact]
    public async Task Create_WithDuplicateTCNo_ReturnsConflict()
    {
        // Arrange
        var dto = CustomerTestData.ValidCreateCustomerDto();
        _mockService.Setup(s => s.CreateCustomerAsync(It.IsAny<CreateCustomerDto>()))
            .ThrowsAsync(new InvalidOperationException("Bu T.C. Kimlik Numarası zaten kayıtlı."));
        
        // ModelState'i valid yap
        _controller.ModelState.Clear();

        // Act
        var result = await _controller.CreateCustomer(dto);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
        var conflictResult = result as ConflictObjectResult;
        conflictResult?.StatusCode.Should().Be(409);
    }

    /// <summary>
    /// Test Case 10: Create - Email Zaten Var (409 Conflict)
    /// Scenario: Aynı email ile müşteri zaten var
    /// Expected: 409 Conflict
    /// </summary>
    [Fact]
    public async Task Create_WithDuplicateEmail_ReturnsConflict()
    {
        // Arrange
        
        // ModelState'i valid yap
        _controller.ModelState.Clear();
        var dto = CustomerTestData.ValidCreateCustomerDto();
        _mockService.Setup(s => s.CreateCustomerAsync(It.IsAny<CreateCustomerDto>()))
            .ThrowsAsync(new InvalidOperationException("Bu e-posta adresi zaten kayıtlı."));

        // Act
        var result = await _controller.CreateCustomer(dto);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    /// <summary>
    /// Test Case 11: Create - Geçersiz FirstName (Boş String)
    /// Scenario: FirstName boş sağlanır
    /// Expected: 400 Bad Request (ModelState validation)
    /// </summary>
    [Fact]
    public async Task Create_WithEmptyFirstName_ReturnsBadRequest()
    {
        // Arrange
        var dto = CustomerTestData.InvalidFirstName();
        // ModelState'i invalid yap
        _controller.ModelState.AddModelError("FirstName", "Ad alanı zorunludur.");

        // Act
        var result = await _controller.CreateCustomer(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        badRequest?.StatusCode.Should().Be(400);
    }

    /// <summary>
    /// Test Case 12: Create - Geçersiz Email Formatı
    /// Scenario: Geçersiz email formatı sağlanır
    /// Expected: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_WithInvalidEmailFormat_ReturnsBadRequest()
    {
        // Arrange
        var dto = CustomerTestData.InvalidEmail();
        _controller.ModelState.AddModelError("Email", "Geçerli bir e-posta adresi girin.");

        // Act
        var result = await _controller.CreateCustomer(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test Case 13: Create - Geçersiz Telefon Numarası
    /// Scenario: Geçersiz phone format sağlanır
    /// Expected: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_WithInvalidPhoneNumber_ReturnsBadRequest()
    {
        // Arrange
        var dto = CustomerTestData.InvalidPhoneNumber();
        _controller.ModelState.AddModelError("PhoneNumber", "Geçerli bir telefon numarası girin.");

        // Act
        var result = await _controller.CreateCustomer(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test Case 14: Create - Geçersiz TCNo (Uzunluk)
    /// Scenario: TCNo 11 hane değil
    /// Expected: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_WithInvalidTCNoLength_ReturnsBadRequest()
    {
        // Arrange
        var dto = CustomerTestData.InvalidIdentityNumber();
        _controller.ModelState.AddModelError("IdentityNumber", "T.C. Kimlik Numarası 11 haneli olmalıdır.");

        // Act
        var result = await _controller.CreateCustomer(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test Case 15: Create - Geçersiz TCNo Formatı (Harf)
    /// Scenario: TCNo harf içeriyor
    /// Expected: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_WithInvalidTCNoFormat_ReturnsBadRequest()
    {
        // Arrange
        var dto = CustomerTestData.InvalidIdentityNumberFormat();
        _controller.ModelState.AddModelError("IdentityNumber", "T.C. Kimlik Numarası sadece sayılardan oluşmalıdır.");

        // Act
        var result = await _controller.CreateCustomer(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test Case 16: Create - Ad Çok Uzun (100+ karakter)
    /// Scenario: FirstName 100 karakterden fazla
    /// Expected: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_WithNameTooLong_ReturnsBadRequest()
    {
        // Arrange
        var dto = CustomerTestData.NameTooLong();
        _controller.ModelState.AddModelError("FirstName", "Ad 2-100 karakter arasında olmalıdır.");

        // Act
        var result = await _controller.CreateCustomer(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test Case 17: Create - Ad Çok Kısa (1 karakter)
    /// Scenario: FirstName 2 karakterden az
    /// Expected: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Create_WithNameTooShort_ReturnsBadRequest()
    {
        // Arrange
        var dto = CustomerTestData.FirstNameTooShort();
        _controller.ModelState.AddModelError("FirstName", "Ad 2-100 karakter arasında olmalıdır.");

        // Act
        var result = await _controller.CreateCustomer(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test Case 18: Create - Service Exception (500)
    /// Scenario: Service'ten beklenmeyen exception
    /// Expected: 500 Internal Server Error
    /// </summary>
    [Fact]
    public async Task Create_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var dto = CustomerTestData.ValidCreateCustomerDto();
        _mockService.Setup(s => s.CreateCustomerAsync(It.IsAny<CreateCustomerDto>()))
            .ThrowsAsync(new Exception("Beklenmeyen hata"));
        
        // ModelState'i valid yap
        _controller.ModelState.Clear();

        // Act
        var result = await _controller.CreateCustomer(dto);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult?.StatusCode.Should().Be(500);
    }

    #endregion

    #region UpdateCustomer Tests

    /// <summary>
    /// Test Case 19: Update - Başarılı Güncelleme
    /// Scenario: Geçerli ID ve DTO ile güncelleme
    /// Expected: 200 OK
    /// </summary>
    [Fact]
    public async Task Update_WithValidIdAndDto_ReturnsOk()
    {
        // Arrange
        var customerId = 1;
        var dto = CustomerTestData.ValidUpdateDto();
        _mockService.Setup(s => s.UpdateCustomerAsync(customerId, It.IsAny<UpdateCustomerDto>())).ReturnsAsync(true);
        
        // ModelState'i valid yap (validation hatası olmadığını simüle et)
        var modelState = _controller.ModelState;
        modelState.Clear();

        // Act
        var result = await _controller.UpdateCustomer(customerId, dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult?.StatusCode.Should().Be(200);
        
        _mockService.Verify(s => s.UpdateCustomerAsync(customerId, It.IsAny<UpdateCustomerDto>()), Times.Once);
    }

    /// <summary>
    /// Test Case 20: Update - Müşteri Bulunamadı (404)
    /// Scenario: Müşteri ID bulunamıyor
    /// Expected: 404 Not Found
    /// </summary>
    [Fact]
    public async Task Update_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var customerId = 999;
        var dto = CustomerTestData.ValidUpdateDto();
        _mockService.Setup(s => s.UpdateCustomerAsync(customerId, It.IsAny<UpdateCustomerDto>())).ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateCustomer(customerId, dto);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult?.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Test Case 21: Update - Geçersiz ID (Negatif)
    /// Scenario: Negatif ID sağlanır
    /// Expected: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Update_WithNegativeId_ReturnsBadRequest()
    {
        // Arrange
        var customerId = -1;
        var dto = CustomerTestData.ValidUpdateDto();

        // Act
        var result = await _controller.UpdateCustomer(customerId, dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test Case 22: Update - Geçersiz Email (Conflict)
    /// Scenario: Email başka müşteri tarafından kullanılıyor
    /// Expected: 409 Conflict
    /// </summary>
    [Fact]
    public async Task Update_WithDuplicateEmail_ReturnsConflict()
    {
        // Arrange
        var customerId = 1;
        var dto = CustomerTestData.ValidUpdateDto();
        _mockService.Setup(s => s.UpdateCustomerAsync(customerId, It.IsAny<UpdateCustomerDto>()))
            .ThrowsAsync(new InvalidOperationException("Bu e-posta adresi zaten başka bir müşteri tarafından kullanılıyor."));
        
        // ModelState'i valid yap
        _controller.ModelState.Clear();

        // Act
        var result = await _controller.UpdateCustomer(customerId, dto);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
        var conflictResult = result as ConflictObjectResult;
        conflictResult?.StatusCode.Should().Be(409);
    }

    /// <summary>
    /// Test Case 23: Update - Geçersiz Email Formatı
    /// Scenario: Email formatı yanlış
    /// Expected: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Update_WithInvalidEmailFormat_ReturnsBadRequest()
    {
        // Arrange
        var customerId = 1;
        var dto = CustomerTestData.InvalidEmailUpdate();
        _controller.ModelState.AddModelError("Email", "Geçerli bir e-posta adresi girin.");

        // Act
        var result = await _controller.UpdateCustomer(customerId, dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test Case 24: Update - Service Exception (500)
    /// Scenario: Service'ten beklenmeyen exception
    /// Expected: 500 Internal Server Error
    /// </summary>
    [Fact]
    public async Task Update_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var customerId = 1;
        var dto = CustomerTestData.ValidUpdateDto();
        _mockService.Setup(s => s.UpdateCustomerAsync(customerId, It.IsAny<UpdateCustomerDto>()))
            .ThrowsAsync(new Exception("Beklenmeyen hata"));
        
        // ModelState'i valid yap
        _controller.ModelState.Clear();

        // Act
        var result = await _controller.UpdateCustomer(customerId, dto);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult?.StatusCode.Should().Be(500);
    }

    #endregion

    #region DeleteCustomer Tests

    /// <summary>
    /// Test Case 25: Delete - Başarılı Silme
    /// Scenario: Geçerli ID ile müşteri silinir
    /// Expected: 200 OK
    /// </summary>
    [Fact]
    public async Task Delete_WithValidId_ReturnsOk()
    {
        // Arrange
        var customerId = 1;
        _mockService.Setup(s => s.DeleteCustomerAsync(customerId)).ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteCustomer(customerId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult?.StatusCode.Should().Be(200);
        
        _mockService.Verify(s => s.DeleteCustomerAsync(customerId), Times.Once);
    }

    /// <summary>
    /// Test Case 26: Delete - Müşteri Bulunamadı (404)
    /// Scenario: Müşteri ID bulunamıyor
    /// Expected: 404 Not Found
    /// </summary>
    [Fact]
    public async Task Delete_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var customerId = 999;
        _mockService.Setup(s => s.DeleteCustomerAsync(customerId)).ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteCustomer(customerId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult?.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Test Case 27: Delete - Geçersiz ID (Negatif)
    /// Scenario: Negatif ID sağlanır
    /// Expected: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Delete_WithNegativeId_ReturnsBadRequest()
    {
        // Arrange
        var customerId = -1;

        // Act
        var result = await _controller.DeleteCustomer(customerId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test Case 28: Delete - Geçersiz ID (Sıfır)
    /// Scenario: Sıfır ID sağlanır
    /// Expected: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Delete_WithZeroId_ReturnsBadRequest()
    {
        // Arrange
        var customerId = 0;

        // Act
        var result = await _controller.DeleteCustomer(customerId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Test Case 29: Delete - Aktif Kredi Var (400)
    /// Scenario: Müşterinin aktif kredisi var, silinemez
    /// Expected: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task Delete_WithActiveLoan_ReturnsBadRequest()
    {
        // Arrange
        var customerId = 1;
        _mockService.Setup(s => s.DeleteCustomerAsync(customerId))
            .ThrowsAsync(new InvalidOperationException("Müşterinin aktif kredisi bulunduğu için silinemez."));

        // Act
        var result = await _controller.DeleteCustomer(customerId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        badRequest?.StatusCode.Should().Be(400);
    }

    /// <summary>
    /// Test Case 30: Delete - Service Exception (500)
    /// Scenario: Service'ten beklenmeyen exception
    /// Expected: 500 Internal Server Error
    /// </summary>
    [Fact]
    public async Task Delete_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var customerId = 1;
        _mockService.Setup(s => s.DeleteCustomerAsync(customerId))
            .ThrowsAsync(new Exception("Beklenmeyen hata"));

        // Act
        var result = await _controller.DeleteCustomer(customerId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult?.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetCustomerSummary Tests

    /// <summary>
    /// Test Case 31: GetSummary - Başarılı
    /// Scenario: Geçerli ID ile müşteri özeti döner
    /// Expected: 200 OK + CustomerSummaryDto
    /// </summary>
    [Fact]
    public async Task GetSummary_WithValidId_ReturnsOkWithSummary()
    {
        // Arrange
        var customerId = 1;
        var summary = CustomerTestData.GetValidCustomerSummaryDto(customerId);
        _mockService.Setup(s => s.GetCustomerSummaryAsync(customerId)).ReturnsAsync(summary);

        // Act
        var result = await _controller.GetCustomerSummary(customerId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<OkObjectResult>();
        
        var okResult = result as OkObjectResult;
        okResult?.StatusCode.Should().Be(200);
        
        var returnedSummary = okResult?.Value as CustomerSummaryDto;
        returnedSummary?.CustomerId.Should().Be(customerId);
        returnedSummary?.FullName.Should().Be("Test Customer");
        returnedSummary?.TotalLoans.Should().Be(2);
        returnedSummary?.ActiveLoans.Should().Be(1);
        
        _mockService.Verify(s => s.GetCustomerSummaryAsync(customerId), Times.Once);
    }

    /// <summary>
    /// Test Case 32: GetSummary - Müşteri Bulunamadı (404)
    /// Scenario: Müşteri ID bulunamıyor
    /// Expected: 404 Not Found
    /// </summary>
    [Fact]
    public async Task GetSummary_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var customerId = 999;
        _mockService.Setup(s => s.GetCustomerSummaryAsync(customerId)).ReturnsAsync((CustomerSummaryDto?)null);

        // Act
        var result = await _controller.GetCustomerSummary(customerId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult?.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Test Case 33: GetSummary - Geçersiz ID (Negatif)
    /// Scenario: Negatif ID sağlanır
    /// Expected: 400 Bad Request
    /// </summary>
    [Fact]
    public async Task GetSummary_WithNegativeId_ReturnsBadRequest()
    {
        // Arrange
        var customerId = -1;

        // Act
        var result = await _controller.GetCustomerSummary(customerId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        badRequest?.StatusCode.Should().Be(400);
    }

    /// <summary>
    /// Test Case 34: GetSummary - Service Exception (500)
    /// Scenario: Service'ten beklenmeyen exception
    /// Expected: 500 Internal Server Error
    /// </summary>
    [Fact]
    public async Task GetSummary_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var customerId = 1;
        _mockService.Setup(s => s.GetCustomerSummaryAsync(customerId))
            .ThrowsAsync(new Exception("Beklenmeyen hata"));

        // Act
        var result = await _controller.GetCustomerSummary(customerId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusCodeResult = result as ObjectResult;
        statusCodeResult?.StatusCode.Should().Be(500);
    }

    #endregion

    #region Edge Cases & Integration Tests

    /// <summary>
    /// Edge Case Test: Multiple Create Calls
    /// Scenario: Ardışık müşteri oluşturma çağrıları
    /// Expected: Her çağrı için ayrı mock invocation
    /// </summary>
    [Fact]
    public async Task Create_MultipleSuccessiveCalls_EachCallIsVerified()
    {
        // Arrange
        var dto1 = CustomerTestData.ValidCreateCustomerDto("user1@test.com", "12345678901");
        var dto2 = CustomerTestData.ValidCreateCustomerDto("user2@test.com", "12345678902");
        
        _mockService.Setup(s => s.CreateCustomerAsync(It.IsAny<CreateCustomerDto>()))
            .ReturnsAsync((CreateCustomerDto d) =>
                d.Email == "user1@test.com" ? 1 : 2);

        // ModelState'i valid yap
        _controller.ModelState.Clear();

        // Act
        var result1 = await _controller.CreateCustomer(dto1);
        var result2 = await _controller.CreateCustomer(dto2);

        // Assert
        result1.Should().BeOfType<CreatedAtActionResult>();
        result2.Should().BeOfType<CreatedAtActionResult>();
        _mockService.Verify(s => s.CreateCustomerAsync(It.IsAny<CreateCustomerDto>()), Times.Exactly(2));
    }

    /// <summary>
    /// Integration Test: CRUD Workflow
    /// Scenario: Create -> GetById -> Update -> Delete
    /// Expected: Her operasyon başarılı
    /// </summary>
    [Fact]
    public async Task CrudWorkflow_CreateGetUpdateDelete_AllSucceed()
    {
        // Arrange
        var createDto = CustomerTestData.ValidCreateCustomerDto();
        var customerId = 1;
        var customer = CustomerTestData.GetValidCustomerResponseDto(customerId);
        var updateDto = CustomerTestData.ValidUpdateDto();

        _mockService.Setup(s => s.CreateCustomerAsync(It.IsAny<CreateCustomerDto>())).ReturnsAsync(customerId);
        
        // ModelState'i valid yap
        _controller.ModelState.Clear();
        _mockService.Setup(s => s.GetCustomerByIdAsync(customerId)).ReturnsAsync(customer);
        _mockService.Setup(s => s.UpdateCustomerAsync(customerId, It.IsAny<UpdateCustomerDto>())).ReturnsAsync(true);
        _mockService.Setup(s => s.DeleteCustomerAsync(customerId)).ReturnsAsync(true);

        // Act & Assert - Create
        var createResult = await _controller.CreateCustomer(createDto);
        createResult.Should().BeOfType<CreatedAtActionResult>();

        // Act & Assert - GetById
        var getResult = await _controller.GetCustomerById(customerId);
        getResult.Should().BeOfType<OkObjectResult>();

        // Act & Assert - Update
        var updateResult = await _controller.UpdateCustomer(customerId, updateDto);
        updateResult.Should().BeOfType<OkObjectResult>();

        // Act & Assert - Delete
        var deleteResult = await _controller.DeleteCustomer(customerId);
        deleteResult.Should().BeOfType<OkObjectResult>();

        // Verify all calls
        _mockService.Verify(s => s.CreateCustomerAsync(It.IsAny<CreateCustomerDto>()), Times.Once);
        _mockService.Verify(s => s.GetCustomerByIdAsync(customerId), Times.Once);
        _mockService.Verify(s => s.UpdateCustomerAsync(customerId, It.IsAny<UpdateCustomerDto>()), Times.Once);
        _mockService.Verify(s => s.DeleteCustomerAsync(customerId), Times.Once);
    }

    /// <summary>
    /// Boundary Test: Valid Phone Number Optional
    /// Scenario: Create müşteri without phone number
    /// Expected: 201 Created (phone optional)
    /// </summary>
    [Fact]
    public async Task Create_WithoutPhoneNumber_ReturnsCreated()
    {
        // Arrange
        var dto = new CustomerTestDataBuilder()
            .WithFirstName("John")
            .WithLastName("Doe")
            .WithIdentityNumber("98765432100")
            .WithEmail("test@example.com")
            .WithPhoneNumber(null) // Phone is optional
            .BuildCreateDto();
        
        _mockService.Setup(s => s.CreateCustomerAsync(It.IsAny<CreateCustomerDto>())).ReturnsAsync(1);
        _controller.ModelState.Clear();

        // Act
        var result = await _controller.CreateCustomer(dto);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    #endregion
}
