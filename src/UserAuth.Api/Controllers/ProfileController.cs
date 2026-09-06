using System.Security.Claims;
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

	public ProfileController(IUserService userService)
	{
		_userService = userService;
	}

	[HttpGet]
	public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
	{
		if (!TryGetUserId(out var userId))
		{
			return Unauthorized();
		}

		var profile = await _userService.GetProfileAsync(userId, cancellationToken);
		return profile is null ? NotFound() : Ok(profile);
	}

	[HttpPut]
	public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request, CancellationToken cancellationToken)
	{
		if (!TryGetUserId(out var userId))
		{
			return Unauthorized();
		}

		var profile = await _userService.UpdateProfileAsync(userId, request, cancellationToken);
		return profile is null ? NotFound() : Ok(profile);
	}

	[HttpPost("change-password")]
	public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request, CancellationToken cancellationToken)
	{
		if (!TryGetUserId(out var userId))
		{
			return Unauthorized();
		}

		var changed = await _userService.ChangePasswordAsync(userId, request, cancellationToken);
		if (!changed)
		{
			return BadRequest(new { message = "Current password is invalid." });
		}

		return Ok(new { message = "Password changed successfully." });
	}

	private bool TryGetUserId(out Guid userId)
	{
		userId = Guid.Empty;

		var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
			?? User.FindFirstValue("sub");

		return !string.IsNullOrWhiteSpace(subject) && Guid.TryParse(subject, out userId);
	}
}
