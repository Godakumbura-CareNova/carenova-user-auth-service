using Microsoft.AspNetCore.Identity;
using UserAuth.Api.DTOs.Auth;
using UserAuth.Api.Exceptions;
using UserAuth.Api.Models;
using UserAuth.Api.Repositories;
using UserAuth.Api.Services.Interfaces;

namespace UserAuth.Api.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginDto request, CancellationToken cancellationToken = default)
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

        var normalizedRole = RoleNames.TryNormalize(user.Role, out var role)
            ? role
            : RoleNames.Patient;

        return new LoginResponseDto
        {
            Token = _tokenService.GenerateToken(user),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(60),
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = normalizedRole
        };
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        // JWT is stateless; secure logout is client-side token disposal after this authorized call.
        return Task.CompletedTask;
    }
}
