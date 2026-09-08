using FluentValidation;
using UserAuth.Api.DTOs.Auth;

namespace UserAuth.Api.Validators;

public class ResetPasswordValidator : AbstractValidator<ResetPasswordDto>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Reset token is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("Password is required.")
            .Must(PasswordPolicy.HasStrongPassword)
            .WithMessage(PasswordPolicy.RequirementsMessage);

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("Please confirm your new password.")
            .Equal(x => x.NewPassword)
            .WithMessage("Passwords do not match.");
    }
}
