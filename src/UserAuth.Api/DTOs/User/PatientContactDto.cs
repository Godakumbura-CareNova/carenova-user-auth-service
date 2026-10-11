namespace UserAuth.Api.DTOs.User;

public sealed record PatientContactDto(Guid PatientId, string FullName, string? MobileNumber);
