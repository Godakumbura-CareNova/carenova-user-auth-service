using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAuth.Api.Models;
using UserAuth.Api.Services;

namespace UserAuth.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleNames.Admin)]
public sealed class AdminDashboardController(AdminUserDashboardService service) : ControllerBase
{
    [HttpGet("api/admin/dashboard/users")]
    public async Task<IActionResult> Users(CancellationToken ct)
    {
        try { return Ok(await service.GetAsync(ct)); }
        catch (DbException) { return StatusCode(503, new { message = "User statistics are unavailable. Please retry." }); }
    }
}
