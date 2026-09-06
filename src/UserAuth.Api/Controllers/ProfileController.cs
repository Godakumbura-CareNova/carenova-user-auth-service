using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAuth.Api.DTOs.Profile;
using UserAuth.Api.Models;
using UserAuth.Api.Services.Interfaces;

namespace UserAuth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RoleNames.AllAuthorizedRoles)]
public class ProfileController : ControllerBase
{
	private readonly IUserService _userService;
	private readonly IProfileService _profileService;
	private readonly IValidator<UpdateProfileDto> _profileValidator;
	private readonly IValidator<ChangePasswordDto> _passwordValidator;

	public ProfileController(
		IUserService userService,
		IProfileService profileService,
		IValidator<UpdateProfileDto> profileValidator,
		IValidator<ChangePasswordDto> passwordValidator)
	{
		_userService = userService;
		_profileService = profileService;
		_profileValidator = profileValidator;
		_passwordValidator = passwordValidator;
	}

	[HttpGet]
	public async Task<IActionResult> Get(CancellationToken cancellationToken)
	{
		if (!TryGetUserId(out var userId))
		{
			return Unauthorized();
		}

		var profile = await _userService.GetProfileAsync(userId, cancellationToken);
		return profile is null ? NotFound() : Ok(profile);
	}

	[HttpPut]
	public async Task<IActionResult> Update([FromBody] UpdateProfileDto request, CancellationToken cancellationToken)
	{
		if (!TryGetUserId(out var userId))
		{
			return Unauthorized();
		}

		var validation = await _profileValidator.ValidateAsync(request, cancellationToken);
		if (!validation.IsValid)
		{
			AddValidationErrors(validation);
			return ValidationProblem(ModelState);
		}

		var profile = await _profileService.UpdateAsync(userId, request, cancellationToken);
		return profile is null ? NotFound() : Ok(profile);
	}

	[HttpPut("password")]
	public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request, CancellationToken cancellationToken)
	{
		if (!TryGetUserId(out var userId))
		{
			return Unauthorized();
		}

		var validation = await _passwordValidator.ValidateAsync(request, cancellationToken);
		if (!validation.IsValid)
		{
			AddValidationErrors(validation);
			return ValidationProblem(ModelState);
		}

		var changed = await _profileService.ChangePasswordAsync(userId, request, cancellationToken);
		return changed ? NoContent() : Unauthorized(new { message = "Current password is incorrect." });
	}

	private bool TryGetUserId(out Guid userId)
	{
		userId = Guid.Empty;

		var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
			?? User.FindFirstValue("sub");

		return !string.IsNullOrWhiteSpace(subject) && Guid.TryParse(subject, out userId);
	}

	private void AddValidationErrors(FluentValidation.Results.ValidationResult validation)
	{
		foreach (var error in validation.Errors)
		{
			ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
		}
	}
}
