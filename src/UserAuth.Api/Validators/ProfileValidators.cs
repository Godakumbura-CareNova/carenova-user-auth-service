using FluentValidation;
using UserAuth.Api.DTOs.Profile;

namespace UserAuth.Api.Validators;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordValidator()
    {
        RuleFor(password => password.CurrentPassword).NotEmpty().WithMessage("Current password is required.");
        RuleFor(password => password.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .Must(PasswordPolicy.HasStrongPassword)
            .WithMessage("Password must be at least 6 characters and include an uppercase letter, lowercase letter, number, and special character.");
        RuleFor(password => password.ConfirmNewPassword)
            .Equal(password => password.NewPassword).WithMessage("Passwords do not match.");
    }
}