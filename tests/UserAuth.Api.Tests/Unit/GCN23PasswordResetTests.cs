using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using UserAuth.Api.Config;
using UserAuth.Api.DTOs.Auth;
using UserAuth.Api.Models;
using UserAuth.Api.Repositories;
using UserAuth.Api.Services;
using UserAuth.Api.Services.Interfaces;
using UserAuth.Api.Validators;

namespace UserAuth.Api.Tests.Unit;

public class GCN23PasswordResetTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher<User>> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IPasswordResetRepository> _passwordResetRepository = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IConfiguration> _configuration = new();

    private AuthService CreateService()
    {
        return new AuthService(
            _userRepository.Object,
            _passwordHasher.Object,
            _tokenService.Object,
            Options.Create(new JwtSettings()),
            _passwordResetRepository.Object,
            _emailService.Object,
            _configuration.Object);
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_UpdatesPasswordAndMarksTokenUsed()
    {
        var userId = Guid.NewGuid();

        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = "valid-token",
            Expiration = DateTime.UtcNow.AddMinutes(20),
            IsUsed = false
        };

        _passwordResetRepository
            .Setup(r => r.GetByTokenAsync(
                "valid-token",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resetToken);

        _passwordHasher
            .Setup(h => h.HashPassword(
                It.IsAny<User>(),
                "NewPass@123"))
            .Returns("new-password-hash");

        var service = CreateService();

        var result = await service.ResetPasswordAsync(
            "valid-token",
            "NewPass@123");

        Assert.Equal(PasswordResetOutcome.Success, result);

        _userRepository.Verify(
            r => r.UpdatePasswordHashAsync(
                userId,
                "new-password-hash",
                It.IsAny<CancellationToken>()),
            Times.Once);

        _passwordResetRepository.Verify(
            r => r.MarkAsUsedAsync(
                resetToken,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_InvalidToken_ReturnsInvalidToken()
    {
        _passwordResetRepository
            .Setup(r => r.GetByTokenAsync(
                "invalid-token",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PasswordResetToken?)null);

        var service = CreateService();

        var result = await service.ResetPasswordAsync(
            "invalid-token",
            "NewPass@123");

        Assert.Equal(PasswordResetOutcome.InvalidToken, result);

        _userRepository.Verify(
            r => r.UpdatePasswordHashAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_ExpiredToken_ReturnsExpiredToken()
    {
        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "expired-token",
            Expiration = DateTime.UtcNow.AddMinutes(-1),
            IsUsed = false
        };

        _passwordResetRepository
            .Setup(r => r.GetByTokenAsync(
                "expired-token",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resetToken);

        var service = CreateService();

        var result = await service.ResetPasswordAsync(
            "expired-token",
            "NewPass@123");

        Assert.Equal(PasswordResetOutcome.ExpiredToken, result);

        _userRepository.Verify(
            r => r.UpdatePasswordHashAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_AlreadyUsedToken_ReturnsTokenAlreadyUsed()
    {
        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "used-token",
            Expiration = DateTime.UtcNow.AddMinutes(20),
            IsUsed = true
        };

        _passwordResetRepository
            .Setup(r => r.GetByTokenAsync(
                "used-token",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resetToken);

        var service = CreateService();

        var result = await service.ResetPasswordAsync(
            "used-token",
            "NewPass@123");

        Assert.Equal(
            PasswordResetOutcome.TokenAlreadyUsed,
            result);

        _userRepository.Verify(
            r => r.UpdatePasswordHashAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_UnknownEmail_DoesNotCreateTokenOrSendEmail()
    {
        _userRepository
            .Setup(r => r.GetByEmailAsync(
                "unknown@example.com",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = CreateService();

        await service.RequestPasswordResetAsync(
            "unknown@example.com");

        _passwordResetRepository.Verify(
            r => r.AddAsync(
                It.IsAny<PasswordResetToken>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _emailService.Verify(
            e => e.SendPasswordResetEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void ResetPasswordValidator_WeakPassword_IsRejected()
    {
        var validator = new ResetPasswordValidator();

        var dto = new ResetPasswordDto
        {
            Token = "valid-token",
            NewPassword = "abc123",
            ConfirmPassword = "abc123"
        };

        var result = validator.Validate(dto);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error =>
                error.PropertyName == nameof(ResetPasswordDto.NewPassword) &&
                error.ErrorMessage == PasswordPolicy.RequirementsMessage);
    }
}
