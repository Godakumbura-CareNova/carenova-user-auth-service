using System.Text.RegularExpressions;

namespace UserAuth.Api.Validators;

public static class PasswordPolicy
{
    public static bool HasStrongPassword(string? password)
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
