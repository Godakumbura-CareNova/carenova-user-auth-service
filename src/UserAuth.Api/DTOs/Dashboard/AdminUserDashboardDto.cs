namespace UserAuth.Api.DTOs.Dashboard;

public sealed record AdminUserDashboardDto(int TotalUsers, int Patients, int PharmacyStaff, int Doctors, DateTime GeneratedAtUtc);
