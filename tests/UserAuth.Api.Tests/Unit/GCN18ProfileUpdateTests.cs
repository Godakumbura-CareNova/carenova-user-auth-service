using Moq;
using UserAuth.Api.DTOs.Profile;
using UserAuth.Api.Models;
using UserAuth.Api.Repositories;
using UserAuth.Api.Services;
using UserAuth.Api.Validators;
using Xunit;

namespace UserAuth.Api.Tests.Unit;

public class GCN18ProfileUpdateTests
{
    private static UpdateProfileDto CreateValidRequest()
    {
        return new UpdateProfileDto
        {
            FullName = "GCN18 Updated Customer",
            MobileNumber = "0779876543",
            LandPhoneNumber = "0112345678",
            Address = "Kandy, Sri Lanka",
            DateOfBirth = DateTime.Today.AddYears(-25),
            Gender = "Other"
        };
    }

    [Fact]
    public async Task UpdateAsync_WithValidProfile_UpdatesAllCustomerFields()
    {
        var userId = Guid.NewGuid();

        var existingUser = new User
        {
            Id = userId,
            FullName = "Old Customer",
            Email = "gcn18.qa@example.com",
            MobileNumber = "0771234567",
            LandPhoneNumber = "0112223333",
            Address = "Colombo, Sri Lanka",
            DateOfBirth = new DateTime(2000, 5, 15),
            Gender = "Male",
            Role = "User"
        };

        var repository = new Mock<IUserRepository>();

        repository
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        repository
            .Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns((User user, CancellationToken _) => Task.FromResult(user));

        var environment = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        var service = new ProfileService(repository.Object, environment.Object);

        var request = new UpdateProfileDto
        {
            FullName = "  GCN18 Updated Customer  ",
            MobileNumber = " 0779876543 ",
            LandPhoneNumber = " 0112345678 ",
            Address = "  Kandy, Sri Lanka  ",
            DateOfBirth = new DateTime(2000, 5, 15),
            Gender = " Other "
        };

        var result = await service.UpdateAsync(userId, request);

        Assert.NotNull(result);
        Assert.Equal("GCN18 Updated Customer", result.FullName);
        Assert.Equal("0779876543", result.MobileNumber);
        Assert.Equal("0112345678", result.LandPhoneNumber);
        Assert.Equal("Kandy, Sri Lanka", result.Address);
        Assert.Equal(new DateTime(2000, 5, 15), result.DateOfBirth);
        Assert.Equal("Other", result.Gender);

        repository.Verify(
            r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenCustomerDoesNotExist_ReturnsNull()
    {
        var repository = new Mock<IUserRepository>();

        repository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var environment = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        var service = new ProfileService(repository.Object, environment.Object);

        var result = await service.UpdateAsync(
            Guid.NewGuid(),
            CreateValidRequest());

        Assert.Null(result);

        repository.Verify(
            r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void Validator_WithValidProfile_PassesValidation()
    {
        var validator = new UpdateProfileValidator();

        var result = validator.Validate(CreateValidRequest());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_WithInvalidMobileNumber_FailsValidation()
    {
        var validator = new UpdateProfileValidator();
        var request = CreateValidRequest();
        request.MobileNumber = "12345";

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(UpdateProfileDto.MobileNumber));
    }

    [Fact]
    public void Validator_WithInvalidLandPhoneNumber_FailsValidation()
    {
        var validator = new UpdateProfileValidator();
        var request = CreateValidRequest();
        request.LandPhoneNumber = "12345";

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(UpdateProfileDto.LandPhoneNumber));
    }

    [Fact]
    public void Validator_WithUserUnderSixteen_FailsValidation()
    {
        var validator = new UpdateProfileValidator();
        var request = CreateValidRequest();
        request.DateOfBirth = DateTime.Today.AddYears(-15);

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(UpdateProfileDto.DateOfBirth)
              && e.ErrorMessage.Contains("at least 16"));
    }

    [Fact]
    public void Validator_WithInvalidGender_FailsValidation()
    {
        var validator = new UpdateProfileValidator();
        var request = CreateValidRequest();
        request.Gender = "InvalidGender";

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(UpdateProfileDto.Gender));
    }

    [Fact]
    public void Validator_WithMissingRequiredProfileFields_FailsValidation()
    {
        var validator = new UpdateProfileValidator();

        var request = new UpdateProfileDto
        {
            FullName = "",
            MobileNumber = "",
            Address = "",
            DateOfBirth = default,
            Gender = ""
        };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(UpdateProfileDto.FullName));

        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(UpdateProfileDto.MobileNumber));

        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(UpdateProfileDto.Address));

        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(UpdateProfileDto.DateOfBirth));

        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(UpdateProfileDto.Gender));
    }
}
