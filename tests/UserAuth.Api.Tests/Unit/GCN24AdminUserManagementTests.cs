using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Moq;
using UserAuth.Api.Controllers;
using UserAuth.Api.DTOs.Auth;
using UserAuth.Api.DTOs.User;
using UserAuth.Api.Exceptions;
using UserAuth.Api.Models;
using UserAuth.Api.Repositories;
using UserAuth.Api.Services;
using UserAuth.Api.Services.Interfaces;
using Xunit;

namespace UserAuth.Api.Tests.Unit;

public class GCN24AdminUserManagementTests
{
    private static User CreateUser(
        string fullName = "GCN24 Test Patient",
        string email = "gcn24.patient@test.com",
        string role = RoleNames.Patient,
        bool isActive = true)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            MobileNumber = "0771234567",
            LandPhoneNumber = "",
            Address = "Colombo",
            DateOfBirth = new DateTime(2000, 1, 1),
            Gender = "Male",
            Role = role,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task GetAllUsersAsync_WhenUsersExist_ShouldReturnAllUsers()
    {
        var repository = new Mock<IUserRepository>();

        repository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>
            {
                CreateUser(),
                CreateUser(
                    "Test Doctor",
                    "doctor@test.com",
                    RoleNames.Doctor)
            });

        var service = new UserService(repository.Object);

        var result = await service.GetAllUsersAsync(null);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllUsersAsync_WhenSearchingByEmail_ShouldReturnMatchingUser()
    {
        var repository = new Mock<IUserRepository>();

        repository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>
            {
                CreateUser(),
                CreateUser(
                    "Another Doctor",
                    "another.doctor@test.com",
                    RoleNames.Doctor)
            });

        var service = new UserService(repository.Object);

        var result = await service.GetAllUsersAsync("GCN24.PATIENT");

        Assert.Single(result);
        Assert.Equal("gcn24.patient@test.com", result[0].Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserExists_ShouldReturnUserDetails()
    {
        var repository = new Mock<IUserRepository>();
        var user = CreateUser();

        repository
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = new UserService(repository.Object);

        var result = await service.GetUserByIdAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal(user.Id.ToString(), result!.Id);
        Assert.Equal("GCN24 Test Patient", result.FullName);
        Assert.Equal(RoleNames.Patient, result.Role);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserDoesNotExist_ShouldReturnNull()
    {
        var repository = new Mock<IUserRepository>();

        repository
            .Setup(r => r.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = new UserService(repository.Object);

        var result = await service.GetUserByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_WhenDoctorRoleRequested_ShouldPersistDoctorRole()
    {
        var repository = new Mock<IUserRepository>();
        var user = CreateUser();

        repository
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        repository
            .Setup(r => r.UpdateAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User updatedUser, CancellationToken _) => updatedUser);

        var service = new UserService(repository.Object);

        var result = await service.UpdateUserRoleAsync(
            user.Id,
            new UpdateRoleDto { Role = "Doctor" });

        Assert.NotNull(result);
        Assert.Equal(RoleNames.Doctor, result!.Role);

        repository.Verify(
            r => r.UpdateAsync(
                It.Is<User>(u => u.Role == RoleNames.Doctor),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_WhenAdminRoleRequested_ShouldRejectAssignment()
    {
        var repository = new Mock<IUserRepository>();
        var user = CreateUser();

        repository
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = new UserService(repository.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateUserRoleAsync(
                user.Id,
                new UpdateRoleDto { Role = "Admin" }));

        Assert.Contains("Admin role cannot be assigned", exception.Message);

        repository.Verify(
            r => r.UpdateAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_WhenUnsupportedRoleRequested_ShouldRejectRole()
    {
        var repository = new Mock<IUserRepository>();
        var user = CreateUser();

        repository
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = new UserService(repository.Object);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateUserRoleAsync(
                user.Id,
                new UpdateRoleDto { Role = "SuperUser" }));

        Assert.Equal("Unsupported role.", exception.Message);
    }

    [Fact]
    public async Task UpdateAccountStatusAsync_WhenDeactivated_ShouldPersistInactiveStatus()
    {
        var repository = new Mock<IUserRepository>();
        var user = CreateUser();

        repository
            .Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        repository
            .Setup(r => r.UpdateAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User updatedUser, CancellationToken _) => updatedUser);

        var service = new UserService(repository.Object);

        var result = await service.UpdateAccountStatusAsync(
            user.Id,
            new UpdateAccountStatusDto { IsActive = false });

        Assert.NotNull(result);
        Assert.False(result!.IsActive);

        repository.Verify(
            r => r.UpdateAsync(
                It.Is<User>(u => !u.IsActive),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WhenAccountIsInactive_ShouldThrowInactiveAccountException()
    {
        var repository = new Mock<IUserRepository>();
        var tokenService = new Mock<ITokenService>();
        var passwordHasher = new PasswordHasher<User>();

        var user = CreateUser(isActive: false);
        user.PasswordHash =
            passwordHasher.HashPassword(user, "Test@123");

        repository
            .Setup(r => r.GetByEmailAsync(
                "gcn24.patient@test.com",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = new AuthService(
            repository.Object,
            passwordHasher,
            tokenService.Object);

        var request = new LoginDto
        {
            Email = "gcn24.patient@test.com",
            Password = "Test@123"
        };

        await Assert.ThrowsAsync<InactiveAccountException>(() =>
            service.LoginAsync(request));
    }

    [Fact]
    public void UsersController_ShouldRequireAdminRole()
    {
        var attribute = typeof(UsersController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(attribute);
        Assert.Equal(RoleNames.Admin, attribute!.Roles);
    }
}
