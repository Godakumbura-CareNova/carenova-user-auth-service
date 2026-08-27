namespace UserAuth.Api.Services.Interfaces;

using UserAuth.Api.DTOs.Auth;

public interface IAuthService
{
	Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}
