using FluentValidation;
using UserAuth.Api.DTOs.Auth;

namespace UserAuth.Api.Validators;

public class LoginValidator : AbstractValidator<LoginDto>
{
	public LoginValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty()
			.WithMessage("Email is required.")
			.EmailAddress()
			.WithMessage("Email must be a valid email address.");

		RuleFor(x => x.Password)
			.NotEmpty()
			.WithMessage("Password is required.");
	}
}
