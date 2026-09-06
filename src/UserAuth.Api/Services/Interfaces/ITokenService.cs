using UserAuth.Api.Models;

namespace UserAuth.Api.Services.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}
