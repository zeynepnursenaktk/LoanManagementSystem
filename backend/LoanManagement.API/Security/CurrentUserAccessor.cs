using System.Security.Claims;
using LoanManagement.Entities;

namespace LoanManagement.API.Security;

public interface ICurrentUserAccessor
{
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    int? CustomerId { get; }
}

public class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public bool IsAdmin => User?.IsInRole(AppRoles.Admin) == true;

    public int? CustomerId
    {
        get
        {
            var v = User?.FindFirst(JwtClaimNames.CustomerId)?.Value;
            return int.TryParse(v, out var id) ? id : null;
        }
    }
}
