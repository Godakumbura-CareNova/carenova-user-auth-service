using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using UserAuth.Api.Config;
using UserAuth.Api.Models;
using UserAuth.Api.Services;

namespace UserAuth.Api.Tests.Unit;

public class TokenServiceTests
{
    [Fact]
    public void GenerateToken_UsesConfiguredClaimsAndSettings()
    {
        var settings = new JwtSettings
        {
            Secret = "a-secure-test-signing-key-that-is-long-enough",
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpiryInMinutes = 60
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "nimal@example.com",
            FullName = "Nimal Perera",
            Role = "User"
        };
        var service = new TokenService(Options.Create(settings));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(service.GenerateToken(user));

        Assert.Equal(settings.Issuer, token.Issuer);
        Assert.Equal(settings.Audience, token.Audiences.Single());
        Assert.Equal(user.Id.ToString(), token.Subject);
        Assert.Equal(user.Email, token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(user.Role, token.Claims.Single(c => c.Type == System.Security.Claims.ClaimTypes.Role).Value);
    }
}
