using System.Text.RegularExpressions;

namespace UserAuth.Api.Validators;

/// <summary>
/// Password strength rule shared by registration (GCN-16) and password reset (GCN-23) —
/// kept in one place so the two stories can't silently drift apart.
/// </summary>
public static class PasswordPolicy
{
    public const string RequirementsMessage =
        "Password must be at least 6 characters and include an uppercase letter, lowercase letter, number, and special character.";

    public static bool IsStrong(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        return password.Length >= 6
            && Regex.IsMatch(password, @"[A-Z]")
            && Regex.IsMatch(password, @"[a-z]")
            && Regex.IsMatch(password, @"\d")
            && Regex.IsMatch(password, @"[^A-Za-z0-9]");
    }
}
