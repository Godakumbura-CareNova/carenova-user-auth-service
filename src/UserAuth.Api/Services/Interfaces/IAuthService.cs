using UserAuth.Api.DTOs.Auth;

namespace UserAuth.Api.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginDto request, CancellationToken cancellationToken = default);
}
