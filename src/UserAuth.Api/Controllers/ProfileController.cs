using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAuth.Api.Services.Interfaces;

namespace UserAuth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "User")]
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
		if (!TryGetAuthenticatedUserId(out var userId))
		{
			return Unauthorized(new { message = "Invalid or missing user identity in token." });
		}

		try
		{
			var profile = await _userService.GetProfileAsync(userId, cancellationToken);
			return profile is null
				? NotFound(new { message = "Customer profile not found." })
				: Ok(profile);
		}
		catch
		{
			return StatusCode(StatusCodes.Status500InternalServerError,
				new { message = "An error occurred while retrieving the profile." });
		}
	}

	private bool TryGetAuthenticatedUserId(out Guid userId)
	{
		userId = Guid.Empty;

		var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
			?? User.FindFirstValue("sub");

		return !string.IsNullOrWhiteSpace(subject) && Guid.TryParse(subject, out userId);
	}
}
