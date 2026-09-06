using FluentValidation;
using UserAuth.Api.DTOs.Auth;

namespace UserAuth.Api.Validators;

public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordDto>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email address is required.")
            .EmailAddress()
            .WithMessage("Email address must be a valid format.");
    }
}
