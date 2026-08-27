using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using UserAuth.Api.Config;
using UserAuth.Api.DTOs.Auth;
using UserAuth.Api.Exceptions;
using UserAuth.Api.Models;
using UserAuth.Api.Repositories;
using UserAuth.Api.Services;
using UserAuth.Api.Services.Interfaces;

namespace UserAuth.Api.Tests.Unit;

public class AuthServiceTests
{
    private static JwtSettings Settings() => new()
    {
        Secret = "a-secure-test-signing-key-that-is-long-enough",
        Issuer = "test-issuer",
        Audience = "test-audience",
        ExpiryInMinutes = 60
    };

    private static User UserFor(string password, bool isActive = true)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "nimal@example.com",
            FullName = "Nimal Perera",
            Role = "User",
            IsActive = isActive
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);
        return user;
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenResponse()
    {
        var user = UserFor("Test@123");
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.GetByEmailAsync("nimal@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(t => t.GenerateToken(user)).Returns("jwt-token");
        var service = new AuthService(repository.Object, new PasswordHasher<User>(), tokenService.Object, Options.Create(Settings()));

        var response = await service.LoginAsync(new LoginRequestDto { Email = user.Email, Password = "Test@123" });

        Assert.NotNull(response);
        Assert.Equal("jwt-token", response.Token);
        tokenService.Verify(t => t.GenerateToken(user), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_DoesNotGenerateToken()
    {
        var user = UserFor("Test@123");
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var tokenService = new Mock<ITokenService>();
        var service = new AuthService(repository.Object, new PasswordHasher<User>(), tokenService.Object, Options.Create(Settings()));

        var response = await service.LoginAsync(new LoginRequestDto { Email = user.Email, Password = "Wrong@123" });

        Assert.Null(response);
        tokenService.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_DoesNotGenerateToken()
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var tokenService = new Mock<ITokenService>();
        var service = new AuthService(repository.Object, new PasswordHasher<User>(), tokenService.Object, Options.Create(Settings()));

        var response = await service.LoginAsync(new LoginRequestDto { Email = "unknown@example.com", Password = "Test@123" });

        Assert.Null(response);
        tokenService.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithInactiveAccount_RejectsWithoutToken()
    {
        var user = UserFor("Test@123", false);
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var tokenService = new Mock<ITokenService>();
        var service = new AuthService(repository.Object, new PasswordHasher<User>(), tokenService.Object, Options.Create(Settings()));

        await Assert.ThrowsAsync<InactiveAccountException>(() => service.LoginAsync(new LoginRequestDto { Email = user.Email, Password = "Test@123" }));
        tokenService.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
    }
}
