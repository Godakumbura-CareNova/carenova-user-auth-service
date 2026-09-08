using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
	private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png" };
	private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png" };
	private const long MaxProfilePictureSizeBytes = 5 * 1024 * 1024;

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
		return profile is null ? NotFound() : Ok(WithAbsolutePictureUrl(profile));
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
		return profile is null ? NotFound() : Ok(WithAbsolutePictureUrl(profile));
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

	[HttpPost("picture")]
	[RequestSizeLimit(MaxProfilePictureSizeBytes + 1024)]
	public async Task<IActionResult> UploadPicture(IFormFile? file, CancellationToken cancellationToken)
	{
		if (!TryGetUserId(out var userId))
		{
			return Unauthorized();
		}

		if (file is null || file.Length == 0)
		{
			return BadRequest(new { message = "Please select a picture to upload." });
		}

		var extension = Path.GetExtension(file.FileName);
		if (string.IsNullOrWhiteSpace(extension)
			|| !AllowedExtensions.Contains(extension)
			|| !AllowedContentTypes.Contains(file.ContentType))
		{
			return BadRequest(new { message = "Unsupported file format. Supported formats are JPG, JPEG, and PNG." });
		}

		if (file.Length > MaxProfilePictureSizeBytes)
		{
			return BadRequest(new { message = "File exceeds the maximum allowed size of 5 MB." });
		}

		try
		{
			await using var stream = file.OpenReadStream();
			var profile = await _profileService.UpdateProfilePictureAsync(userId, stream, extension.ToLowerInvariant(), cancellationToken);
			return profile is null ? NotFound() : Ok(WithAbsolutePictureUrl(profile));
		}
		catch
		{
			return StatusCode(StatusCodes.Status500InternalServerError,
				new { message = "Failed to upload the profile picture. Please try again." });
		}
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

	private ProfileDto WithAbsolutePictureUrl(ProfileDto profile)
	{
		if (profile.HasProfilePicture
			&& !string.IsNullOrWhiteSpace(profile.ProfilePictureUrl)
			&& !profile.ProfilePictureUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
		{
			profile.ProfilePictureUrl = $"{Request.Scheme}://{Request.Host}{profile.ProfilePictureUrl}";
		}

		return profile;
	}
}
