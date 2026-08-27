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
    Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);
    Task<PasswordResetOutcome> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default);
}
