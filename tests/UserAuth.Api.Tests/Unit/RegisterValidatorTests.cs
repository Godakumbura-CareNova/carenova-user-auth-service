using UserAuth.Api.DTOs.Auth;
using UserAuth.Api.Validators;

namespace UserAuth.Api.Tests.Unit;

public class RegisterValidatorTests
{
    private readonly RegisterValidator _validator = new();

    [Fact]
    public void Validate_WhenRequestIsValid_ShouldNotHaveErrors()
    {
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

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_WhenFirstNameIsMissing_ShouldHaveError(string? firstName)
    {
        var request = new RegisterRequestDto
        {
            FirstName = firstName!,
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

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequestDto.FirstName));
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("user@")]
    public void Validate_WhenEmailIsInvalid_ShouldHaveError(string email)
    {
        var request = new RegisterRequestDto
        {
            FirstName = "Nimal",
            LastName = "Perera",
            Email = email,
            Password = "Test@123",
            ConfirmPassword = "Test@123",
            MobileNumber = "+94771234567",
            LandPhoneNumber = "+94112233445",
            Address = "No. 12, Colombo 03",
            DateOfBirth = new DateTime(1998, 5, 10),
            Gender = "Male"
        };

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequestDto.Email));
    }

    [Theory]
    [InlineData("abc123")]
    [InlineData("password")]
    [InlineData("weak@123")]
    public void Validate_WhenPasswordDoesNotMeetComplexityRequirements_ShouldHaveError(string password)
    {
        var request = new RegisterRequestDto
        {
            FirstName = "Nimal",
            LastName = "Perera",
            Email = "nimal.perera@example.com",
            Password = password,
            ConfirmPassword = password,
            MobileNumber = "+94771234567",
            LandPhoneNumber = "+94112233445",
            Address = "No. 12, Colombo 03",
            DateOfBirth = new DateTime(1998, 5, 10),
            Gender = "Male"
        };

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequestDto.Password));
    }

    [Fact]
    public void Validate_WhenPasswordAndConfirmPasswordDoNotMatch_ShouldHaveError()
    {
        var request = new RegisterRequestDto
        {
            FirstName = "Nimal",
            LastName = "Perera",
            Email = "nimal.perera@example.com",
            Password = "StrongP@ss1",
            ConfirmPassword = "StrongP@ss2",
            MobileNumber = "+94771234567",
            LandPhoneNumber = "+94112233445",
            Address = "No. 12, Colombo 03",
            DateOfBirth = new DateTime(1998, 5, 10),
            Gender = "Male"
        };

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequestDto.ConfirmPassword));
    }

    [Theory]
    [InlineData("077123456")]
    [InlineData("94771234567")]
    [InlineData("07123456789")]
    public void Validate_WhenMobileNumberIsInvalid_ShouldHaveError(string mobileNumber)
    {
        var request = new RegisterRequestDto
        {
            FirstName = "Nimal",
            LastName = "Perera",
            Email = "nimal.perera@example.com",
            Password = "Test@123",
            ConfirmPassword = "Test@123",
            MobileNumber = mobileNumber,
            LandPhoneNumber = "+94112233445",
            Address = "No. 12, Colombo 03",
            DateOfBirth = new DateTime(1998, 5, 10),
            Gender = "Male"
        };

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequestDto.MobileNumber));
    }

    [Fact]
    public void Validate_WhenDateOfBirthIsTooYoung_ShouldHaveError()
    {
        var request = new RegisterRequestDto
        {
            FirstName = "Asha",
            LastName = "Silva",
            Email = "asha@example.com",
            Password = "Test@123",
            ConfirmPassword = "Test@123",
            MobileNumber = "0771234567",
            LandPhoneNumber = "0112233445",
            Address = "No. 5, Galle",
            DateOfBirth = DateTime.Today.AddYears(-15),
            Gender = "Female"
        };

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequestDto.DateOfBirth));
    }
}
