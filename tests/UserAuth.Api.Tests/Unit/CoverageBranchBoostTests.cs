using Microsoft.AspNetCore.Identity;
using Moq;
using UserAuth.Api.DTOs.Auth;
using UserAuth.Api.DTOs.Profile;
using UserAuth.Api.DTOs.User;
using UserAuth.Api.Models;
using UserAuth.Api.Repositories;
using UserAuth.Api.Services;

namespace UserAuth.Api.Tests.Unit;

public class CoverageBranchBoostTests
{
    // ---------------- ROLENAMES ----------------

    [Theory]
    [InlineData(null, false, RoleNames.Patient)]
    [InlineData("", false, RoleNames.Patient)]
    [InlineData("   ", false, RoleNames.Patient)]
    [InlineData("User", true, RoleNames.Patient)]
    [InlineData(" patient ", true, RoleNames.Patient)]
    [InlineData("PharmacyStaff", true, RoleNames.PharmacyStaff)]
    [InlineData(" Pharmacy Staff ", true, RoleNames.PharmacyStaff)]
    [InlineData("doctor", true, RoleNames.Doctor)]
    [InlineData("ADMIN", true, RoleNames.Admin)]
    [InlineData("UnknownRole", false, RoleNames.Patient)]
    public void TryNormalize_AllRolePaths(
        string? input,
        bool expectedResult,
        string expectedRole)
    {
        var result = RoleNames.TryNormalize(input, out var role);

        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedRole, role);
    }

    [Theory]
    [InlineData(RoleNames.Patient, true)]
    [InlineData(RoleNames.PharmacyStaff, true)]
    [InlineData(RoleNames.Doctor, true)]
    [InlineData(RoleNames.Admin, false)]
    [InlineData("Unknown", false)]
    public void IsAssignableRole_AllPaths(
        string role,
        bool expected)
    {
        Assert.Equal(expected, RoleNames.IsAssignableRole(role));
    }

    // ---------------- GET PROFILE ----------------

    [Fact]
    public async Task GetProfileAsync_UserMissing_ReturnsNull()
    {
        var repo = new Mock<IUserRepository>();

        repo.Setup(r => r.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = new UserService(repo.Object);

        var result = await service.GetProfileAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProfileAsync_UserExists_MapsOptionalValues()
    {
        var id = Guid.NewGuid();

        var repo = new Mock<IUserRepository>();

        repo.Setup(r => r.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = id,
                FullName = "Test User",
                Email = "test@test.com",
                MobileNumber = "0771234567",
                LandPhoneNumber = " ",
                Address = "",
                Gender = null!,
                DateOfBirth = default,
                ProfilePicturePath = null
            });

        var service = new UserService(repo.Object);

        var result = await service.GetProfileAsync(id);

        Assert.NotNull(result);
        Assert.Null(result.LandPhoneNumber);
        Assert.Null(result.Address);
        Assert.Null(result.Gender);
        Assert.Null(result.DateOfBirth);
        Assert.False(result.HasProfilePicture);
    }

    // ---------------- UPDATE PROFILE ----------------

    [Fact]
    public async Task UpdateProfileAsync_UserMissing_ReturnsNull()
    {
        var repo = new Mock<IUserRepository>();

        repo.Setup(r => r.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = new UserService(repo.Object);

        var result = await service.UpdateProfileAsync(
            Guid.NewGuid(),
            new UpdateProfileDto());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateProfileAsync_UserExists_UpdatesProfile()
    {
        var id = Guid.NewGuid();

        var user = new User
        {
            Id = id,
            FullName = "Old",
            Email = "test@test.com"
        };

        var repo = new Mock<IUserRepository>();

        repo.Setup(r => r.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        repo.Setup(r => r.UpdateAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);

        var service = new UserService(repo.Object);

        var result = await service.UpdateProfileAsync(
            id,
            new UpdateProfileDto
            {
                FullName = " New Name ",
                MobileNumber = " 0771234567 ",
                LandPhoneNumber = null,
                Address = " Colombo ",
                DateOfBirth = new DateTime(2000, 1, 1),
                Gender = " Male "
            });

        Assert.NotNull(result);
        Assert.Equal("New Name", result.FullName);
        Assert.Equal(string.Empty, user.LandPhoneNumber);
    }

    // ---------------- CHANGE PASSWORD ----------------

    [Fact]
    public async Task ChangePasswordAsync_UserMissing_ReturnsFalse()
    {
        var repo = new Mock<IUserRepository>();

        repo.Setup(r => r.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = new UserService(repo.Object);

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
    public async Task ChangePasswordAsync_BlankHash_ReturnsFalse()
    {
        var repo = new Mock<IUserRepository>();

        repo.Setup(r => r.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                PasswordHash = ""
            });

        var service = new UserService(repo.Object);

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
    public async Task ChangePasswordAsync_WrongPassword_ReturnsFalse()
    {
        var repo = new Mock<IUserRepository>();
        var hasher = new Mock<IPasswordHasher<User>>();

        repo.Setup(r => r.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                PasswordHash = "hash"
            });

        hasher.Setup(h => h.VerifyHashedPassword(
                It.IsAny<User>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(PasswordVerificationResult.Failed);

        var service =
            new UserService(repo.Object, hasher.Object);

        var result = await service.ChangePasswordAsync(
            Guid.NewGuid(),
            new ChangePasswordDto
            {
                CurrentPassword = "wrong",
                NewPassword = "New@123"
            });

        Assert.False(result);
    }

    [Fact]
    public async Task ChangePasswordAsync_CorrectPassword_ReturnsTrue()
    {
        var id = Guid.NewGuid();

        var user = new User
        {
            Id = id,
            PasswordHash = "old-hash"
        };

        var repo = new Mock<IUserRepository>();
        var hasher = new Mock<IPasswordHasher<User>>();

        repo.Setup(r => r.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        hasher.Setup(h => h.VerifyHashedPassword(
                user,
                "old-hash",
                "Old@123"))
            .Returns(PasswordVerificationResult.Success);

        hasher.Setup(h => h.HashPassword(
                user,
                "New@123"))
            .Returns("new-hash");

        repo.Setup(r => r.UpdateAsync(
                user,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service =
            new UserService(repo.Object, hasher.Object);

        var result = await service.ChangePasswordAsync(
            id,
            new ChangePasswordDto
            {
                CurrentPassword = " Old@123 ",
                NewPassword = " New@123 "
            });

        Assert.True(result);
        Assert.Equal("new-hash", user.PasswordHash);
    }

    // ---------------- GET ALL USERS ----------------

    [Fact]
    public async Task GetAllUsersAsync_NoSearch_ReturnsAll()
    {
        var repo = new Mock<IUserRepository>();

        repo.Setup(r => r.GetAllAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>
            {
                CreateUser("Alice", "alice@test.com"),
                CreateUser("Bob", "bob@test.com")
            });

        var service = new UserService(repo.Object);

        var result = await service.GetAllUsersAsync(null);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllUsersAsync_SearchMatchesName()
    {
        var repo = new Mock<IUserRepository>();

        repo.Setup(r => r.GetAllAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>
            {
                CreateUser("Alice Patient", "alice@test.com"),
                CreateUser("Bob Doctor", "bob@test.com")
            });

        var service = new UserService(repo.Object);

        var result = await service.GetAllUsersAsync(" alice ");

        Assert.Single(result);
        Assert.Equal("Alice Patient", result[0].FullName);
    }

    [Fact]
    public async Task GetAllUsersAsync_SearchMatchesEmail()
    {
        var repo = new Mock<IUserRepository>();

        repo.Setup(r => r.GetAllAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>
            {
                CreateUser("Alice", "alice@test.com"),
                CreateUser("Bob", "doctor@hospital.com")
            });

        var service = new UserService(repo.Object);

        var result = await service.GetAllUsersAsync("hospital");

        Assert.Single(result);
        Assert.Equal("doctor@hospital.com", result[0].Email);
    }

    [Fact]
    public async Task GetAllUsersAsync_UnknownRole_DefaultsPatient()
    {
        var repo = new Mock<IUserRepository>();

        var user = CreateUser("Unknown Role", "unknown@test.com");
        user.Role = "SomethingElse";
        user.LandPhoneNumber = "";
        user.Address = " ";
        user.Gender = "";
        user.DateOfBirth = default;

        repo.Setup(r => r.GetAllAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { user });

        var service = new UserService(repo.Object);

        var result = await service.GetAllUsersAsync("");

        Assert.Single(result);
        Assert.Equal(RoleNames.Patient, result[0].Role);
        Assert.Null(result[0].LandPhoneNumber);
        Assert.Null(result[0].Address);
        Assert.Null(result[0].Gender);
        Assert.Null(result[0].DateOfBirth);
    }

    // ---------------- GET USER ----------------

    [Fact]
    public async Task GetUserByIdAsync_Missing_ReturnsNull()
    {
        var repo = new Mock<IUserRepository>();

        repo.Setup(r => r.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = new UserService(repo.Object);

        Assert.Null(
            await service.GetUserByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetUserByIdAsync_Found_ReturnsUser()
    {
        var id = Guid.NewGuid();

        var repo = new Mock<IUserRepository>();

        repo.Setup(r => r.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser("Found", "found@test.com", id));

        var service = new UserService(repo.Object);

        var result = await service.GetUserByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal("Found", result.FullName);
    }

    // ---------------- UPDATE ROLE ----------------

    [Fact]
    public async Task UpdateUserRoleAsync_MissingUser_ReturnsNull()
    {
        var repo = new Mock<IUserRepository>();

        repo.Setup(r => r.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = new UserService(repo.Object);

        var result = await service.UpdateUserRoleAsync(
            Guid.NewGuid(),
            new UpdateRoleDto { Role = "Doctor" });

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_UnsupportedRole_Throws()
    {
        var id = Guid.NewGuid();

        var repo = new Mock<IUserRepository>();

        repo.Setup(r => r.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser("Test", "test@test.com", id));

        var service = new UserService(repo.Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateUserRoleAsync(
                id,
                new UpdateRoleDto { Role = "InvalidRole" }));
    }

    [Fact]
    public async Task UpdateUserRoleAsync_AdminRole_Throws()
    {
        var id = Guid.NewGuid();

        var repo = new Mock<IUserRepository>();

        repo.Setup(r => r.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser("Test", "test@test.com", id));

        var service = new UserService(repo.Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateUserRoleAsync(
                id,
                new UpdateRoleDto { Role = "Admin" }));
    }

    [Fact]
    public async Task UpdateUserRoleAsync_PharmacyStaffWithSpace_Normalizes()
    {
        var id = Guid.NewGuid();

        var user = CreateUser("Test", "test@test.com", id);

        var repo = new Mock<IUserRepository>();

        repo.Setup(r => r.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        repo.Setup(r => r.UpdateAsync(
                user,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = new UserService(repo.Object);

        var result = await service.UpdateUserRoleAsync(
            id,
            new UpdateRoleDto
            {
                Role = " Pharmacy Staff "
            });

        Assert.NotNull(result);
        Assert.Equal(RoleNames.PharmacyStaff, result.Role);
    }

    // ---------------- ACCOUNT STATUS ----------------

    [Fact]
    public async Task UpdateAccountStatusAsync_Missing_ReturnsNull()
    {
        var repo = new Mock<IUserRepository>();

        repo.Setup(r => r.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = new UserService(repo.Object);

        var result = await service.UpdateAccountStatusAsync(
            Guid.NewGuid(),
            new UpdateAccountStatusDto
            {
                IsActive = false
            });

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAccountStatusAsync_Found_UpdatesStatus()
    {
        var id = Guid.NewGuid();

        var user = CreateUser("Test", "test@test.com", id);

        var repo = new Mock<IUserRepository>();

        repo.Setup(r => r.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        repo.Setup(r => r.UpdateAsync(
                user,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = new UserService(repo.Object);

        var result = await service.UpdateAccountStatusAsync(
            id,
            new UpdateAccountStatusDto
            {
                IsActive = false
            });

        Assert.NotNull(result);
        Assert.False(result.IsActive);
    }

    // ---------------- DELETE ----------------

    [Fact]
    public async Task DeleteUserAsync_Missing_ReturnsFalse()
    {
        var repo = new Mock<IUserRepository>();

        repo.Setup(r => r.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var service = new UserService(repo.Object);

        Assert.False(
            await service.DeleteUserAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteUserAsync_Found_ReturnsRepositoryResult()
    {
        var id = Guid.NewGuid();

        var user = CreateUser("Delete", "delete@test.com", id);

        var repo = new Mock<IUserRepository>();

        repo.Setup(r => r.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        repo.Setup(r => r.DeleteAsync(
                user,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new UserService(repo.Object);

        Assert.True(await service.DeleteUserAsync(id));
    }

    // ---------------- REGISTRATION BRANCH ----------------

    [Fact]
    public async Task RegisterAsync_FullNameProvided_UsesFullName()
    {
        var repo = new Mock<IUserRepository>();

        repo.Setup(r => r.EmailExistsAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        repo.Setup(r => r.AddAsync(
                It.IsAny<User>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (User u, CancellationToken _) => u);

        var service = new UserService(repo.Object);

        var result = await service.RegisterAsync(
            new RegisterRequestDto
            {
                FullName = " Direct Name ",
                FirstName = "Ignored",
                LastName = "Ignored",
                Email = " direct@test.com ",
                Password = " Test@123 ",
                ConfirmPassword = "Test@123",
                MobileNumber = " 0771234567 ",
                LandPhoneNumber = " 0112345678 ",
                Address = " Colombo ",
                DateOfBirth = new DateTime(2000, 1, 1),
                Gender = " Male "
            });

        Assert.Equal("Direct Name", result.FullName);
        Assert.Equal("direct@test.com", result.Email);
    }

    private static User CreateUser(
        string name,
        string email,
        Guid? id = null)
    {
        return new User
        {
            Id = id ?? Guid.NewGuid(),
            FullName = name,
            Email = email,
            MobileNumber = "0771234567",
            LandPhoneNumber = "0112345678",
            Address = "Colombo",
            DateOfBirth = new DateTime(2000, 1, 1),
            Gender = "Male",
            Role = RoleNames.Patient,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }
}
