using Moq;
using UserAuth.Api.DTOs.Auth;
using UserAuth.Api.Exceptions;
using UserAuth.Api.Models;
using UserAuth.Api.Repositories;
using UserAuth.Api.Services;

namespace UserAuth.Api.Tests.Unit;

public class UserServiceTests
{
    [Fact]
    public async Task RegisterAsync_WhenRequestIsValid_ShouldCreateUser()
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.EmailExistsAsync("nimal.perera@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repository.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User user, CancellationToken _) =>
            {
                user.Id = Guid.NewGuid();
                return user;
            });

        var service = new UserService(repository.Object);
        var request = new RegisterRequestDto
        {
            FirstName = "Nimal",
            LastName = "Perera",
            Email = "nimal.perera@example.com",
            Password = "Test@123",
            ConfirmPassword = "Test@123",
            MobileNumber = "+94771234567",
            LandPhoneNumber = "+94112233445",
            Address = "No. 12, Colombo 03",
            DateOfBirth = new DateTime(1998, 5, 10),
            Gender = "Male"
        };

        var response = await service.RegisterAsync(request);

        Assert.Equal("Nimal Perera", response.FullName);
        Assert.Equal("nimal.perera@example.com", response.Email);
        Assert.Equal("Male", response.Gender);
        var createdUser = repository.Invocations
            .Single(invocation => invocation.Method.Name == nameof(IUserRepository.AddAsync))
            .Arguments[0] as User;
        Assert.NotNull(createdUser?.PasswordHash);
        Assert.NotEqual(request.Password, createdUser.PasswordHash);
        repository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ShouldThrowDuplicateEmailException()
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.EmailExistsAsync("existing@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = new UserService(repository.Object);
        var request = new RegisterRequestDto
        {
            FirstName = "Existing",
            LastName = "User",
            Email = "existing@example.com",
            Password = "Test@123",
            ConfirmPassword = "Test@123",
            MobileNumber = "+94771234567",
            LandPhoneNumber = "+94112233445",
            Address = "No. 1, Kandy",
            DateOfBirth = new DateTime(1990, 6, 12),
            Gender = "Female"
        };

        await Assert.ThrowsAsync<DuplicateEmailException>(() => service.RegisterAsync(request));
        repository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
