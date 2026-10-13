using Microsoft.EntityFrameworkCore;
using UserAuth.Api.Clients;
using UserAuth.Api.Data;
using UserAuth.Api.DTOs.User;
using UserAuth.Api.Models;
using UserAuth.Api.Services.Interfaces;

namespace UserAuth.Api.Services;

public sealed class DoctorPatientContactService(AppDbContext db, AppointmentAccessClient access) : IDoctorPatientContactService
{
    public async Task<IReadOnlyList<PatientContactDto>> GetContactsAsync(IReadOnlyCollection<Guid> patientIds, string bearerToken, CancellationToken ct)
    {
        var assigned = await access.GetAssignedPatientIdsAsync(patientIds, bearerToken, ct);
        // Project only the contact fields; full profiles and credentials never leave this service.
        return await db.Users.AsNoTracking().Where(user => assigned.Contains(user.Id) && (user.Role == RoleNames.Patient || user.Role == RoleNames.LegacyUser))
            .Select(user => new PatientContactDto(user.Id, user.FullName, user.MobileNumber)).ToListAsync(ct);
    }
}
