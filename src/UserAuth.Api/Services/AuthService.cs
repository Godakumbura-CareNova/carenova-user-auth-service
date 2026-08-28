using Microsoft.AspNetCore.Identity;
using UserAuth.Api.DTOs.Auth;
using UserAuth.Api.Repositories;
using UserAuth.Api.Services.Interfaces;

namespace UserAuth.Api.Services;

public class AuthService : IAuthService
{
	private readonly IUserRepository _userRepository;
	private readonly ITokenService _tokenService;

	public AuthService(IUserRepository userRepository, ITokenService tokenService)
	{
		_userRepository = userRepository;
		_tokenService = tokenService;
	}

	public async Task<LoginResponseDto?> LoginAsync(LoginDto request, CancellationToken cancellationToken = default)
	{
		var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
		if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash) || !user.IsActive)
		{
			return null;
		}

		var passwordHasher = new PasswordHasher<Models.User>();
		var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
		if (verification == PasswordVerificationResult.Failed)
		{
			return null;
		}

		var tokenData = _tokenService.GenerateToken(user);
		return new LoginResponseDto
		{
			Token = tokenData.Token,
			ExpiresAtUtc = tokenData.ExpiresAtUtc,
			UserId = user.Id,
			Email = user.Email,
			FullName = user.FullName,
			Role = user.Role
		};
	}
}
