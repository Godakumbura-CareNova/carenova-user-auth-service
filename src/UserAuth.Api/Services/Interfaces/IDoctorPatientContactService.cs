using UserAuth.Api.DTOs.User;

namespace UserAuth.Api.Services.Interfaces;

public interface IDoctorPatientContactService
{
    Task<IReadOnlyList<PatientContactDto>> GetContactsAsync(IReadOnlyCollection<Guid> patientIds, string bearerToken, CancellationToken ct);
}
