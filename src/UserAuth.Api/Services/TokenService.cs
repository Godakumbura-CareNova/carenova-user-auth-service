using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using UserAuth.Api.Config;
using UserAuth.Api.Models;
using UserAuth.Api.Services.Interfaces;

namespace UserAuth.Api.Services;

public class TokenService : ITokenService
{
	private readonly JwtSettings _jwtSettings;

	public TokenService(IConfiguration configuration)
	{
		_jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
			?? throw new InvalidOperationException("JwtSettings configuration is missing.");
	}

	public (string Token, DateTime ExpiresAtUtc) GenerateToken(User user)
	{
		var now = DateTime.UtcNow;
		var expires = now.AddMinutes(_jwtSettings.ExpiryInMinutes <= 0 ? 60 : _jwtSettings.ExpiryInMinutes);

		var claims = new List<Claim>
		{
			new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
			new(ClaimTypes.NameIdentifier, user.Id.ToString()),
			new(JwtRegisteredClaimNames.Email, user.Email),
			new(ClaimTypes.Email, user.Email),
			new(ClaimTypes.Name, user.FullName),
			new(ClaimTypes.Role, user.Role),
			new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
		};

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var token = new JwtSecurityToken(
			issuer: _jwtSettings.Issuer,
			audience: _jwtSettings.Audience,
			claims: claims,
			notBefore: now,
			expires: expires,
			signingCredentials: creds);

		return (new JwtSecurityTokenHandler().WriteToken(token), expires);
	}
}
