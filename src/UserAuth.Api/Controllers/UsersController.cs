using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAuth.Api.DTOs.User;
using UserAuth.Api.Models;
using UserAuth.Api.Services.Interfaces;

namespace UserAuth.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = RoleNames.Admin)]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllUsers([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllUsersAsync(search, cancellationToken);
        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserByIdAsync(id, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPatch("{id:guid}/role")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleDto request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userService.UpdateUserRoleAsync(id, request, cancellationToken);
            return user is null ? NotFound() : Ok(user);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateAccountStatusDto request, CancellationToken cancellationToken)
    {
        var user = await _userService.UpdateAccountStatusAsync(id, request, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _userService.DeleteUserAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
