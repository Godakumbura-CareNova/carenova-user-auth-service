using UserAuth.Api.Models;

namespace UserAuth.Api.Services.Interfaces;

using UserAuth.Api.Models;

public interface ITokenService
{
    string GenerateToken(User user);
}
