using UserAuth.Api.DTOs.Auth;

namespace UserAuth.Api.Services.Interfaces;

public enum PasswordResetOutcome
{
    Success,
    InvalidToken,
    ExpiredToken,
    TokenAlreadyUsed,
}

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);
    Task<PasswordResetOutcome> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default);
}
