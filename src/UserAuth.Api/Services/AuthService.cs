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
	private readonly IUserRepository _userRepository;
	private readonly IPasswordHasher<User> _passwordHasher;
	private readonly ITokenService _tokenService;
	private readonly JwtSettings _jwtSettings;

	public AuthService(
		IUserRepository userRepository,
		IPasswordHasher<User> passwordHasher,
		ITokenService tokenService,
		IOptions<JwtSettings> jwtOptions)
	{
		_userRepository = userRepository;
		_passwordHasher = passwordHasher;
		_tokenService = tokenService;
		_jwtSettings = jwtOptions.Value;
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
}
