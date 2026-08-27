using FluentValidation;
using UserAuth.Api.DTOs.Auth;

namespace UserAuth.Api.Validators;

public class LoginValidator : AbstractValidator<LoginRequestDto>
{
	public LoginValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty()
			.EmailAddress();
		RuleFor(x => x.Password)
			.NotEmpty();
	}
}
