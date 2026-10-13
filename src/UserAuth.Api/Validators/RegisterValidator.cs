using FluentValidation;
using UserAuth.Api.DTOs.Auth;

namespace UserAuth.Api.Validators;

public class RegisterValidator : AbstractValidator<RegisterDto>
{
    public RegisterValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required.");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email address is required.")
            .EmailAddress()
            .WithMessage("Email address must be a valid format.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .Must(PasswordPolicy.HasStrongPassword)
            .WithMessage("Password must be at least 6 characters and include an uppercase letter, lowercase letter, number, and special character.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("Please confirm your password.")
            .Equal(x => x.Password)
            .WithMessage("Passwords do not match.");

        RuleFor(x => x.MobileNumber)
            .NotEmpty()
            .WithMessage("Mobile number is required.")
            .Matches(@"^(?:\+94|0)7[0-9]{8}$")
            .WithMessage("Mobile number must be a valid Sri Lankan format.");

        RuleFor(x => x.LandPhoneNumber)
            .NotEmpty()
            .WithMessage("Land phone number is required.")
            .Matches(@"^(?:\+94|0)[1-9][0-9]{8}$")
            .WithMessage("Land phone number must be a valid Sri Lankan format.");

        RuleFor(x => x.Address)
            .NotEmpty()
            .WithMessage("Address is required.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty()
            .WithMessage("Date of birth is required.")
            .LessThan(DateTime.Today)
            .WithMessage("Date of birth must be in the past.")
            .Must(BeAtLeastSixteenYearsOld)
            .WithMessage("User must be at least 16 years old.");

        RuleFor(x => x.Gender)
            .NotEmpty()
            .WithMessage("Gender is required.")
            .Must(g => g == "Male" || g == "Female" || g == "Other")
            .WithMessage("Gender must be Male, Female, or Other.");
    }

    private static bool BeAtLeastSixteenYearsOld(DateTime dateOfBirth)
    {
        if (dateOfBirth == default)
        {
            return false;
        }

        var age = DateTime.Today.Year - dateOfBirth.Year;

        if (dateOfBirth.Date > DateTime.Today.AddYears(-age))
        {
            age--;
        }

        return age >= 16;
    }
}
