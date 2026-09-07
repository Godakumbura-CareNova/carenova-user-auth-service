using System.Security.Claims;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using UserAuth.Api.Config;
using UserAuth.Api.Controllers;
using UserAuth.Api.DTOs.Auth;
using UserAuth.Api.DTOs.Profile;
using UserAuth.Api.DTOs.User;
using UserAuth.Api.Exceptions;
using UserAuth.Api.Models;
using UserAuth.Api.Repositories;
using UserAuth.Api.Services;
using UserAuth.Api.Services.Interfaces;

namespace UserAuth.Api.Tests.Unit;

public class CoverageBoostTests
{
    private static ValidationResult Valid() => new();

    private static ValidationResult Invalid(string property = "Field") =>
        new(new[] { new ValidationFailure(property, "Invalid value") });

    private static AuthController CreateAuthController(
        Mock<IUserService>? users = null,
        Mock<IAuthService>? auth = null,
        ValidationResult? registerValidation = null,
        ValidationResult? loginValidation = null,
        ValidationResult? forgotValidation = null,
        ValidationResult? resetValidation = null)
    {
        users ??= new Mock<IUserService>();
        auth ??= new Mock<IAuthService>();

        var registerValidator = new Mock<IValidator<RegisterRequestDto>>();
        var loginValidator = new Mock<IValidator<LoginDto>>();
        var forgotValidator = new Mock<IValidator<ForgotPasswordDto>>();
        var resetValidator = new Mock<IValidator<ResetPasswordDto>>();

        registerValidator
            .Setup(v => v.ValidateAsync(
                It.IsAny<RegisterRequestDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(registerValidation ?? Valid());

        loginValidator
            .Setup(v => v.ValidateAsync(
                It.IsAny<LoginDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(loginValidation ?? Valid());

        forgotValidator
            .Setup(v => v.ValidateAsync(
                It.IsAny<ForgotPasswordDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(forgotValidation ?? Valid());

        resetValidator
            .Setup(v => v.ValidateAsync(
                It.IsAny<ResetPasswordDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resetValidation ?? Valid());

        var controller = new AuthController(
            users.Object,
            auth.Object,
            registerValidator.Object,
            loginValidator.Object,
            forgotValidator.Object,
            resetValidator.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    private static ProfileController CreateProfileController(
        Mock<IUserService>? users = null,
        Mock<IProfileService>? profiles = null,
        ValidationResult? profileValidation = null,
        ValidationResult? passwordValidation = null,
        Guid? userId = null)
    {
        users ??= new Mock<IUserService>();
        profiles ??= new Mock<IProfileService>();

        var profileValidator = new Mock<IValidator<UpdateProfileDto>>();
        var passwordValidator = new Mock<IValidator<ChangePasswordDto>>();

        profileValidator
            .Setup(v => v.ValidateAsync(
                It.IsAny<UpdateProfileDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(profileValidation ?? Valid());

        passwordValidator
            .Setup(v => v.ValidateAsync(
                It.IsAny<ChangePasswordDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(passwordValidation ?? Valid());

        var http = new DefaultHttpContext();
        http.Request.Scheme = "https";
        http.Request.Host = new HostString("localhost");

        if (userId.HasValue)
        {
            http.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(
                            ClaimTypes.NameIdentifier,
                            userId.Value.ToString())
                    },
                    "TestAuth"));
        }

        var controller = new ProfileController(
            users.Object,
            profiles.Object,
            profileValidator.Object,
            passwordValidator.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = http
        };

        return controller;
    }

    // ---------------- AUTH CONTROLLER ----------------

    [Fact]
    public async Task Register_InvalidRequest_ReturnsValidationProblem()
    {
        var controller = CreateAuthController(
            registerValidation: Invalid("Email"));

        var result = await controller.Register(
            new RegisterRequestDto(),
            CancellationToken.None);

        Assert.IsType<ObjectResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var users = new Mock<IUserService>();

        users.Setup(x => x.RegisterAsync(
                It.IsAny<RegisterRequestDto>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DuplicateEmailException("duplicate"));

        var controller = CreateAuthController(users);

        var result = await controller.Register(
            new RegisterRequestDto(),
            CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Login_InvalidRequest_ReturnsValidationProblem()
    {
        var controller = CreateAuthController(
            loginValidation: Invalid("Email"));

        var result = await controller.Login(
            new LoginDto(),
            CancellationToken.None);

        Assert.IsType<ObjectResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        var auth = new Mock<IAuthService>();

        auth.Setup(x => x.LoginAsync(
                It.IsAny<LoginDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoginResponseDto?)null);

        var controller = CreateAuthController(auth: auth);

        var result = await controller.Login(
            new LoginDto(),
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_InactiveAccount_ReturnsForbidden()
    {
        var auth = new Mock<IAuthService>();

        auth.Setup(x => x.LoginAsync(
                It.IsAny<LoginDto>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new InactiveAccountException("inactive"));

        var controller = CreateAuthController(auth: auth);

        var result = await controller.Login(
            new LoginDto(),
            CancellationToken.None);

        var response = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_InvalidRequest_ReturnsValidationProblem()
    {
        var controller = CreateAuthController(
            forgotValidation: Invalid("Email"));

        var result = await controller.ForgotPassword(
            new ForgotPasswordDto(),
            CancellationToken.None);

        Assert.IsType<ObjectResult>(result);
    }

    [Fact]
    public async Task ForgotPassword_ValidRequest_ReturnsOk()
    {
        var auth = new Mock<IAuthService>();

        var controller = CreateAuthController(auth: auth);

        var result = await controller.ForgotPassword(
            new ForgotPasswordDto
            {
                Email = "user@test.com"
            },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);

        auth.Verify(x => x.RequestPasswordResetAsync(
                "user@test.com",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(PasswordResetOutcome.Success, 200)]
    [InlineData(PasswordResetOutcome.ExpiredToken, 400)]
    [InlineData(PasswordResetOutcome.TokenAlreadyUsed, 400)]
    [InlineData(PasswordResetOutcome.InvalidToken, 400)]
    public async Task ResetPassword_AllOutcomes_ReturnExpectedStatus(
        PasswordResetOutcome outcome,
        int expectedStatus)
    {
        var auth = new Mock<IAuthService>();

        auth.Setup(x => x.ResetPasswordAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(outcome);

        var controller = CreateAuthController(auth: auth);

        var result = await controller.ResetPassword(
            new ResetPasswordDto
            {
                Token = "token",
                NewPassword = "Test@123"
            },
            CancellationToken.None);

        var status = result switch
        {
            OkObjectResult ok => ok.StatusCode ?? 200,
            BadRequestObjectResult bad => bad.StatusCode ?? 400,
            ObjectResult obj => obj.StatusCode ?? 0,
            _ => 0
        };

        Assert.Equal(expectedStatus, status);
    }

    [Fact]
    public async Task ResetPassword_InvalidValidation_ReturnsValidationProblem()
    {
        var controller = CreateAuthController(
            resetValidation: Invalid("Token"));

        var result = await controller.ResetPassword(
            new ResetPasswordDto(),
            CancellationToken.None);

        Assert.IsType<ObjectResult>(result);
    }

    // ---------------- PROFILE CONTROLLER ----------------

    [Fact]
    public async Task ProfileGet_NoUserClaim_ReturnsUnauthorized()
    {
        var controller = CreateProfileController();

        var result = await controller.Get(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task ProfileGet_UserMissing_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        var users = new Mock<IUserService>();

        users.Setup(x => x.GetProfileAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProfileDto?)null);

        var controller = CreateProfileController(
            users: users,
            userId: id);

        var result = await controller.Get(CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ProfileGet_WithRelativePicture_ReturnsAbsoluteUrl()
    {
        var id = Guid.NewGuid();
        var users = new Mock<IUserService>();

        users.Setup(x => x.GetProfileAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfileDto
            {
                Id = id,
                HasProfilePicture = true,
                ProfilePictureUrl = "/uploads/test.jpg"
            });

        var controller = CreateProfileController(
            users: users,
            userId: id);

        var result = await controller.Get(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<ProfileDto>(ok.Value);

        Assert.Equal(
            "https://localhost/uploads/test.jpg",
            profile.ProfilePictureUrl);
    }

    [Fact]
    public async Task ProfileUpdate_InvalidValidation_ReturnsValidationProblem()
    {
        var controller = CreateProfileController(
            profileValidation: Invalid("FullName"),
            userId: Guid.NewGuid());

        var result = await controller.Update(
            new UpdateProfileDto(),
            CancellationToken.None);

        Assert.IsType<ObjectResult>(result);
    }

    [Fact]
    public async Task ProfileUpdate_UserMissing_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        var profiles = new Mock<IProfileService>();

        profiles.Setup(x => x.UpdateAsync(
                id,
                It.IsAny<UpdateProfileDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProfileDto?)null);

        var controller = CreateProfileController(
            profiles: profiles,
            userId: id);

        var result = await controller.Update(
            new UpdateProfileDto(),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ChangePassword_InvalidValidation_ReturnsValidationProblem()
    {
        var controller = CreateProfileController(
            passwordValidation: Invalid("CurrentPassword"),
            userId: Guid.NewGuid());

        var result = await controller.ChangePassword(
            new ChangePasswordDto(),
            CancellationToken.None);

        Assert.IsType<ObjectResult>(result);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsUnauthorized()
    {
        var id = Guid.NewGuid();
        var profiles = new Mock<IProfileService>();

        profiles.Setup(x => x.ChangePasswordAsync(
                id,
                It.IsAny<ChangePasswordDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var controller = CreateProfileController(
            profiles: profiles,
            userId: id);

        var result = await controller.ChangePassword(
            new ChangePasswordDto(),
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task ChangePassword_Success_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        var profiles = new Mock<IProfileService>();

        profiles.Setup(x => x.ChangePasswordAsync(
                id,
                It.IsAny<ChangePasswordDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = CreateProfileController(
            profiles: profiles,
            userId: id);

        var result = await controller.ChangePassword(
            new ChangePasswordDto(),
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UploadPicture_NoFile_ReturnsBadRequest()
    {
        var controller = CreateProfileController(
            userId: Guid.NewGuid());

        var result = await controller.UploadPicture(
            null,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadPicture_UnsupportedFormat_ReturnsBadRequest()
    {
        var controller = CreateProfileController(
            userId: Guid.NewGuid());

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var file = new FormFile(
            stream,
            0,
            stream.Length,
            "file",
            "picture.gif")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/gif"
        };

        var result = await controller.UploadPicture(
            file,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadPicture_TooLarge_ReturnsBadRequest()
    {
        var controller = CreateProfileController(
            userId: Guid.NewGuid());

        using var stream =
            new MemoryStream(new byte[5 * 1024 * 1024 + 1]);

        var file = new FormFile(
            stream,
            0,
            stream.Length,
            "file",
            "picture.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };

        var result = await controller.UploadPicture(
            file,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadPicture_ServiceReturnsNull_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        var profiles = new Mock<IProfileService>();

        profiles.Setup(x => x.UpdateProfilePictureAsync(
                id,
                It.IsAny<Stream>(),
                ".jpg",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProfileDto?)null);

        var controller = CreateProfileController(
            profiles: profiles,
            userId: id);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var file = new FormFile(
            stream,
            0,
            stream.Length,
            "file",
            "picture.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };

        var result = await controller.UploadPicture(
            file,
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UploadPicture_ServiceThrows_Returns500()
    {
        var id = Guid.NewGuid();
        var profiles = new Mock<IProfileService>();

        profiles.Setup(x => x.UpdateProfilePictureAsync(
                id,
                It.IsAny<Stream>(),
                ".jpg",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("disk error"));

        var controller = CreateProfileController(
            profiles: profiles,
            userId: id);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var file = new FormFile(
            stream,
            0,
            stream.Length,
            "file",
            "picture.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };

        var result = await controller.UploadPicture(
            file,
            CancellationToken.None);

        var response = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, response.StatusCode);
    }

    // ---------------- AUTH SERVICE ----------------

    [Fact]
    public async Task AuthService_Login_UserMissing_ReturnsNull()
    {
        var repo = new Mock<IUserRepository>();

        repo.Setup(x => x.GetByEmailAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = CreateAuthService(repo: repo);

        var result = await service.LoginAsync(
            new LoginDto
            {
                Email = " missing@test.com ",
                Password = " Test@123 "
            });

        Assert.Null(result);
    }

    [Fact]
    public async Task AuthService_Login_BadPassword_ReturnsNull()
    {
        var repo = new Mock<IUserRepository>();
        var hasher = new Mock<IPasswordHasher<User>>();

        repo.Setup(x => x.GetByEmailAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Email = "test@test.com",
                PasswordHash = "hash",
                IsActive = true
            });

        hasher.Setup(x => x.VerifyHashedPassword(
                It.IsAny<User>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(PasswordVerificationResult.Failed);

        var service = CreateAuthService(repo, hasher);

        var result = await service.LoginAsync(
            new LoginDto
            {
                Email = "test@test.com",
                Password = "wrong"
            });

        Assert.Null(result);
    }

    [Fact]
    public async Task AuthService_Login_UnknownRole_DefaultsToPatient()
    {
        var repo = new Mock<IUserRepository>();
        var hasher = new Mock<IPasswordHasher<User>>();
        var tokens = new Mock<ITokenService>();

        repo.Setup(x => x.GetByEmailAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                FullName = "Test User",
                PasswordHash = "hash",
                Role = "UnknownRole",
                IsActive = true
            });

        hasher.Setup(x => x.VerifyHashedPassword(
                It.IsAny<User>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(PasswordVerificationResult.Success);

        tokens.Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns("jwt-token");

        var service = CreateAuthService(repo, hasher, tokens);

        var result = await service.LoginAsync(
            new LoginDto
            {
                Email = "test@test.com",
                Password = "Test@123"
            });

        Assert.NotNull(result);
        Assert.Equal(RoleNames.Patient, result.Role);
    }

    [Fact]
    public async Task AuthService_RequestReset_UserMissing_DoesNothing()
    {
        var repo = new Mock<IUserRepository>();
        var resetRepo = new Mock<IPasswordResetRepository>();
        var email = new Mock<IEmailService>();

        repo.Setup(x => x.GetByEmailAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = CreateAuthService(
            repo: repo,
            resetRepo: resetRepo,
            email: email);

        await service.RequestPasswordResetAsync(
            "missing@test.com");

        resetRepo.Verify(x => x.AddAsync(
                It.IsAny<PasswordResetToken>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        email.Verify(x => x.SendPasswordResetEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static AuthService CreateAuthService(
        Mock<IUserRepository>? repo = null,
        Mock<IPasswordHasher<User>>? hasher = null,
        Mock<ITokenService>? tokens = null,
        Mock<IPasswordResetRepository>? resetRepo = null,
        Mock<IEmailService>? email = null)
    {
        repo ??= new Mock<IUserRepository>();
        hasher ??= new Mock<IPasswordHasher<User>>();
        tokens ??= new Mock<ITokenService>();
        resetRepo ??= new Mock<IPasswordResetRepository>();
        email ??= new Mock<IEmailService>();

        var configuration = new Mock<IConfiguration>();

        return new AuthService(
            repo.Object,
            hasher.Object,
            tokens.Object,
            Options.Create(new JwtSettings
            {
                ExpiryInMinutes = 60
            }),
            resetRepo.Object,
            email.Object,
            configuration.Object);
    }

    // ---------------- PROFILE SERVICE ----------------

    [Fact]
    public async Task ProfileService_ChangePassword_UserMissing_ReturnsFalse()
    {
        var repo = new Mock<IUserRepository>();

        repo.Setup(x => x.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var environment =
            new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();

        var service =
            new ProfileService(repo.Object, environment.Object);

        var result = await service.ChangePasswordAsync(
            Guid.NewGuid(),
            new ChangePasswordDto
            {
                CurrentPassword = "Old@123",
                NewPassword = "New@123"
            });

        Assert.False(result);
    }

    [Fact]
    public async Task ProfileService_ChangePassword_ValidPassword_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var user = new User
        {
            Id = id,
            Email = "profile@test.com"
        };

        var passwordHasher = new PasswordHasher<User>();
        user.PasswordHash =
            passwordHasher.HashPassword(user, "Old@123");

        var repo = new Mock<IUserRepository>();

        repo.Setup(x => x.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        repo.Setup(x => x.UpdateAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (User u, CancellationToken _) => u);

        var environment =
            new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();

        var service =
            new ProfileService(repo.Object, environment.Object);

        var result = await service.ChangePasswordAsync(
            id,
            new ChangePasswordDto
            {
                CurrentPassword = "Old@123",
                NewPassword = "New@123"
            });

        Assert.True(result);
    }


    // ---------------- FINAL 85% BOOST ----------------

    [Fact]
    public async Task ProfileUpdate_NoUserClaim_ReturnsUnauthorized()
    {
        var controller = CreateProfileController();

        var result = await controller.Update(
            new UpdateProfileDto(),
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task ChangePassword_NoUserClaim_ReturnsUnauthorized()
    {
        var controller = CreateProfileController();

        var result = await controller.ChangePassword(
            new ChangePasswordDto(),
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task UploadPicture_NoUserClaim_ReturnsUnauthorized()
    {
        var controller = CreateProfileController();

        var result = await controller.UploadPicture(
            null,
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task UploadPicture_EmptyFile_ReturnsBadRequest()
    {
        var controller = CreateProfileController(
            userId: Guid.NewGuid());

        using var stream = new MemoryStream();

        var file = new FormFile(
            stream,
            0,
            0,
            "file",
            "picture.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };

        var result = await controller.UploadPicture(
            file,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadPicture_NoExtension_ReturnsBadRequest()
    {
        var controller = CreateProfileController(
            userId: Guid.NewGuid());

        using var stream =
            new MemoryStream(new byte[] { 1, 2, 3 });

        var file = new FormFile(
            stream,
            0,
            stream.Length,
            "file",
            "picture")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };

        var result = await controller.UploadPicture(
            file,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadPicture_ValidExtensionWrongContentType_ReturnsBadRequest()
    {
        var controller = CreateProfileController(
            userId: Guid.NewGuid());

        using var stream =
            new MemoryStream(new byte[] { 1, 2, 3 });

        var file = new FormFile(
            stream,
            0,
            stream.Length,
            "file",
            "picture.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        var result = await controller.UploadPicture(
            file,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadPicture_Success_ReturnsOkWithAbsoluteUrl()
    {
        var id = Guid.NewGuid();

        var profiles = new Mock<IProfileService>();

        profiles.Setup(x => x.UpdateProfilePictureAsync(
                id,
                It.IsAny<Stream>(),
                ".png",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfileDto
            {
                Id = id,
                HasProfilePicture = true,
                ProfilePictureUrl = "/uploads/profile.png"
            });

        var controller = CreateProfileController(
            profiles: profiles,
            userId: id);

        using var stream =
            new MemoryStream(new byte[] { 1, 2, 3 });

        var file = new FormFile(
            stream,
            0,
            stream.Length,
            "file",
            "picture.PNG")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var result = await controller.UploadPicture(
            file,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<ProfileDto>(ok.Value);

        Assert.Equal(
            "https://localhost/uploads/profile.png",
            profile.ProfilePictureUrl);
    }

    [Fact]
    public async Task ProfileGet_AbsolutePictureUrl_RemainsUnchanged()
    {
        var id = Guid.NewGuid();

        var users = new Mock<IUserService>();

        users.Setup(x => x.GetProfileAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfileDto
            {
                Id = id,
                HasProfilePicture = true,
                ProfilePictureUrl =
                    "https://cdn.example.com/profile.jpg"
            });

        var controller = CreateProfileController(
            users: users,
            userId: id);

        var result =
            await controller.Get(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<ProfileDto>(ok.Value);

        Assert.Equal(
            "https://cdn.example.com/profile.jpg",
            profile.ProfilePictureUrl);
    }

    [Fact]
    public async Task ProfileGet_NoPicture_DoesNotBuildAbsoluteUrl()
    {
        var id = Guid.NewGuid();

        var users = new Mock<IUserService>();

        users.Setup(x => x.GetProfileAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfileDto
            {
                Id = id,
                HasProfilePicture = false,
                ProfilePictureUrl = null
            });

        var controller = CreateProfileController(
            users: users,
            userId: id);

        var result =
            await controller.Get(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<ProfileDto>(ok.Value);

        Assert.Null(profile.ProfilePictureUrl);
    }



    // ---------------- FINAL LINE 85 BOOST ----------------

    [Fact]
    public async Task Logout_ReturnsOkAndDisablesCache()
    {
        var auth = new Mock<IAuthService>();

        auth.Setup(x => x.LogoutAsync(
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = CreateAuthController(auth: auth);

        var result = await controller.Logout(
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);

        Assert.Equal(
            "no-store",
            controller.Response.Headers.CacheControl.ToString());

        auth.Verify(x => x.LogoutAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProfileService_GetAsync_UserMissing_ReturnsNull()
    {
        var repo = new Mock<IUserRepository>();

        repo.Setup(x => x.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var environment =
            new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();

        var service =
            new ProfileService(repo.Object, environment.Object);

        var result =
            await service.GetAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task ProfileService_GetAsync_UserFound_ReturnsProfile()
    {
        var id = Guid.NewGuid();

        var repo = new Mock<IUserRepository>();

        repo.Setup(x => x.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = id,
                FullName = "Coverage User",
                Email = "coverage@test.com",
                MobileNumber = "0771234567",
                LandPhoneNumber = "",
                Address = "Colombo",
                Gender = "Male",
                DateOfBirth = new DateTime(2000, 1, 1),
                ProfilePicturePath = "/uploads/test.jpg"
            });

        var environment =
            new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();

        var service =
            new ProfileService(repo.Object, environment.Object);

        var result = await service.GetAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("coverage@test.com", result.Email);
        Assert.True(result.HasProfilePicture);
    }

}
