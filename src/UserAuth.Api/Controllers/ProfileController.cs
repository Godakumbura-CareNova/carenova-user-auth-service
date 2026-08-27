using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UserAuth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
	[HttpGet]
	public IActionResult GetProfile()
	{
		return Ok(new
		{
			UserId = User.FindFirst("sub")?.Value,
			Email = User.FindFirst("email")?.Value,
			Name = User.Identity?.Name,
			Role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
		});
	}
}
