using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAuth.Api.DTOs.User;
using UserAuth.Api.Models;
using UserAuth.Api.Services.Interfaces;

namespace UserAuth.Api.Controllers;

[ApiController]
[Route("api/doctor/patient-contacts")]
[Authorize(Roles = RoleNames.Doctor)]
public sealed class DoctorPatientContactsController(IDoctorPatientContactService service, ILogger<DoctorPatientContactsController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> GetContacts(PatientContactsRequestDto request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var doctorId) || doctorId == Guid.Empty) return Unauthorized();
        if (!request.IsValid) return BadRequest(new { message = "Supply 1 to 100 unique, valid patient IDs." });
        try { return Ok(await service.GetContactsAsync(request.PatientIds!, Request.Headers.Authorization.ToString(), ct)); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { logger.LogWarning("Patient contact verification unavailable ({FailureType})", ex.GetType().Name); return StatusCode(503, new { message = "Could not retrieve patient contact information. Please try again." }); }
    }
}
