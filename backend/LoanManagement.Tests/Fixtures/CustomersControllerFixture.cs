using Moq;
using LoanManagement.Business.Abstract;
using LoanManagement.API.Controllers;
using LoanManagement.API.Security;

namespace LoanManagement.Tests.Fixtures;

/// <summary>
/// CustomersController test fixture - Setup ve Teardown
/// </summary>
public class CustomersControllerFixture : IDisposable
{
    public Mock<ICustomerService> MockCustomerService { get; }
    public Mock<ICurrentUserAccessor> MockCurrentUser { get; }
    public CustomersController Controller { get; }

    public CustomersControllerFixture()
    {
        // Mock service'i oluştur
        MockCustomerService = new Mock<ICustomerService>();
        MockCurrentUser = new Mock<ICurrentUserAccessor>();
        MockCurrentUser.Setup(x => x.IsAdmin).Returns(true);
        MockCurrentUser.Setup(x => x.CustomerId).Returns((int?)null);
        MockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        // Controller'ı mock service ile başlat
        Controller = new CustomersController(MockCustomerService.Object, MockCurrentUser.Object);
    }

    /// <summary>
    /// Tüm mock'ları sıfırla (Arrange aşaması öncesi)
    /// </summary>
    public void ResetMocks()
    {
        MockCustomerService.Reset();
    }

    /// <summary>
    /// Test bittikten sonra temizle
    /// </summary>
    public void Dispose()
    {
        MockCustomerService?.Invocations.Clear();
    }
}

/// <summary>
/// xUnit Collection Fixture - Test class'ları arasında fixture'ı paylaş
/// </summary>
[CollectionDefinition("CustomersController Collection")]
public class CustomersControllerCollection : ICollectionFixture<CustomersControllerFixture>
{
    // Bu class'ın tek amacı fixture'ı tanımlamak
}
