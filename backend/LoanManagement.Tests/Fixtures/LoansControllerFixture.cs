using Moq;
using LoanManagement.Business.Abstract;
using LoanManagement.API.Controllers;
using LoanManagement.API.Security;

namespace LoanManagement.Tests.Fixtures;

public class LoansControllerFixture : IDisposable
{
    public Mock<ILoanService> MockLoanService { get; }
    public Mock<ICurrentUserAccessor> MockCurrentUser { get; }
    public LoansController Controller { get; }

    public LoansControllerFixture()
    {
        MockLoanService = new Mock<ILoanService>();
        MockCurrentUser = new Mock<ICurrentUserAccessor>();
        MockCurrentUser.Setup(x => x.IsAdmin).Returns(true);
        MockCurrentUser.Setup(x => x.CustomerId).Returns((int?)null);
        MockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);
        Controller = new LoansController(MockLoanService.Object, MockCurrentUser.Object);
    }

    public void ResetMocks()
    {
        MockLoanService.Reset();
    }

    public void Dispose()
    {
        MockLoanService?.Invocations.Clear();
    }
}

[CollectionDefinition("LoansController Collection")]
public class LoansControllerCollection : ICollectionFixture<LoansControllerFixture>
{
}
