namespace UserAuth.Api.Models;

public static class RoleNames
{
    public const string Patient = "Patient";
    public const string PharmacyStaff = "PharmacyStaff";
    public const string Doctor = "Doctor";
    public const string Admin = "Admin";

    public const string PharmacyStaffWithSpace = "Pharmacy Staff";
    public const string LegacyUser = "User";

    public const string AllAuthorizedRoles = "Patient,PharmacyStaff,Pharmacy Staff,Doctor,Admin";

    public static bool TryNormalize(string? inputRole, out string normalizedRole)
    {
        normalizedRole = Patient;
        if (string.IsNullOrWhiteSpace(inputRole))
        {
            return false;
        }

        var role = inputRole.Trim();

        if (role.Equals(LegacyUser, StringComparison.OrdinalIgnoreCase)
            || role.Equals(Patient, StringComparison.OrdinalIgnoreCase))
        {
            normalizedRole = Patient;
            return true;
        }

        if (role.Equals(PharmacyStaff, StringComparison.OrdinalIgnoreCase)
            || role.Equals(PharmacyStaffWithSpace, StringComparison.OrdinalIgnoreCase))
        {
            normalizedRole = PharmacyStaff;
            return true;
        }

        if (role.Equals(Doctor, StringComparison.OrdinalIgnoreCase))
        {
            normalizedRole = Doctor;
            return true;
        }

        if (role.Equals(Admin, StringComparison.OrdinalIgnoreCase))
        {
            normalizedRole = Admin;
            return true;
        }

        return false;
    }
}