using LoanManagement.Entities.DTOs;

namespace LoanManagement.Business.Abstract;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    Task<LoginResponseDto> RegisterAsync(RegisterRequestDto request);
}
