using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using UserAuth.Api.Config;
using UserAuth.Api.DTOs.Auth;
using UserAuth.Api.Exceptions;
using UserAuth.Api.Models;
using UserAuth.Api.Repositories;
using UserAuth.Api.Services.Interfaces;

namespace UserAuth.Api.Services;

public class AuthService : IAuthService
{
    private const int ResetLinkExpiryMinutes = 30;

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;
    private readonly IPasswordResetRepository _passwordResetRepository;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtOptions,
        IPasswordResetRepository passwordResetRepository,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _jwtSettings = jwtOptions.Value;
        _passwordResetRepository = passwordResetRepository;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.Trim(), cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return null;
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password.Trim());
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (!user.IsActive)
        {
            throw new InactiveAccountException("This account is inactive.");
        }

        return new LoginResponseDto
        {
            Token = _tokenService.GenerateToken(user),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryInMinutes),
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role
        };
    }

    public async Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default)
    {
        // Deliberately no distinction in behavior between "found" and "not found" that's
        // observable to the caller — this method either does real work or silently no-ops,
        // and the controller returns the identical response either way (Scenario 5).
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            return;
        }

        var token = GenerateSecureToken();
        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = token,
            Expiration = DateTime.UtcNow.AddMinutes(ResetLinkExpiryMinutes),
            IsUsed = false,
        };

        await _passwordResetRepository.AddAsync(resetToken, cancellationToken);

        var frontendBaseUrl = _configuration["FrontendBaseUrl"] ?? "http://localhost:3000";
        var resetLink = $"{frontendBaseUrl}/reset-password?token={token}";

        await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink, cancellationToken);
    }

    public async Task<PasswordResetOutcome> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default)
    {
        var resetToken = await _passwordResetRepository.GetByTokenAsync(token, cancellationToken);
        if (resetToken is null)
        {
            return PasswordResetOutcome.InvalidToken;
        }

        if (resetToken.IsUsed)
        {
            return PasswordResetOutcome.TokenAlreadyUsed;
        }

        if (resetToken.Expiration < DateTime.UtcNow)
        {
            return PasswordResetOutcome.ExpiredToken;
        }

        var newPasswordHash = _passwordHasher.HashPassword(new User(), newPassword);

        await _userRepository.UpdatePasswordHashAsync(resetToken.UserId, newPasswordHash, cancellationToken);
        await _passwordResetRepository.MarkAsUsedAsync(resetToken, cancellationToken);

        return PasswordResetOutcome.Success;
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}
