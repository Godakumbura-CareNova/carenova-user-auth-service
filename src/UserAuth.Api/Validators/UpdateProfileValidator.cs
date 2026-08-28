using FluentValidation;
using UserAuth.Api.DTOs.Profile;

namespace UserAuth.Api.Validators;

public class UpdateProfileValidator : AbstractValidator<UpdateProfileDto>
{
    public UpdateProfileValidator()
    {
        RuleFor(profile => profile.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MinimumLength(2).WithMessage("Full name must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Full name must not exceed 200 characters.");
        RuleFor(profile => profile.MobileNumber)
            .NotEmpty().WithMessage("Mobile number is required.")
            .Matches(@"^(?:\+94|0)7[0-9]{8}$").WithMessage("Mobile number must be a valid Sri Lankan format.");
        RuleFor(profile => profile.LandPhoneNumber)
            .Matches(@"^(?:\+94|0)[1-9][0-9]{8}$").When(profile => !string.IsNullOrWhiteSpace(profile.LandPhoneNumber))
            .WithMessage("Land phone number must be a valid Sri Lankan format.");
        RuleFor(profile => profile.Address).NotEmpty().WithMessage("Address is required.").MaximumLength(500);
        RuleFor(profile => profile.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .LessThan(DateTime.Today).WithMessage("Date of birth must be in the past.")
            .Must(BeAtLeastSixteenYearsOld).WithMessage("User must be at least 16 years old.");
        RuleFor(profile => profile.Gender)
            .NotEmpty().WithMessage("Gender is required.")
            .Must(gender => gender == "Male" || gender == "Female" || gender == "Other")
            .WithMessage("Gender must be Male, Female, or Other.");
    }

    private static bool BeAtLeastSixteenYearsOld(DateTime dateOfBirth)
    {
        var age = DateTime.Today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > DateTime.Today.AddYears(-age))
        {
            age--;
        }

        return age >= 16;
    }
}
