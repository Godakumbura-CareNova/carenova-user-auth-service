using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace UserAuth.Api.Clients;

public sealed class AppointmentAccessClient(HttpClient http)
{
    public async Task<IReadOnlyList<Guid>> GetAssignedPatientIdsAsync(IReadOnlyCollection<Guid> patientIds, string bearerToken, CancellationToken ct)
    {
        if (http.BaseAddress is null || !AuthenticationHeaderValue.TryParse(bearerToken, out var authorization) || authorization.Scheme != "Bearer" || string.IsNullOrWhiteSpace(authorization.Parameter))
            throw new HttpRequestException("Appointment verification is unavailable.");
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/doctor/appointments/patient-access") { Content = JsonContent.Create(new { patientIds }) };
        request.Headers.Authorization = authorization;
        using var response = await http.SendAsync(request, ct);
        if (response.StatusCode != System.Net.HttpStatusCode.OK) throw new HttpRequestException("Appointment verification failed.");
        var access = await response.Content.ReadFromJsonAsync<PatientAccessResponse>(cancellationToken: ct);
        if (access?.PatientIds is not { } ids || ids.Length > patientIds.Count || ids.Distinct().Count() != ids.Length || ids.Any(id => id == Guid.Empty || !patientIds.Contains(id)))
            throw new HttpRequestException("Invalid appointment verification response.");
        return ids;
    }
    private sealed class PatientAccessResponse { public Guid[]? PatientIds { get; set; } }
}
