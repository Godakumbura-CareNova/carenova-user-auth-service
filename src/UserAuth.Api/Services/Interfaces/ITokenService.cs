using UserAuth.Api.Models;

namespace UserAuth.Api.Services.Interfaces;

public interface ITokenService
{
	(string Token, DateTime ExpiresAtUtc) GenerateToken(User user);
}
