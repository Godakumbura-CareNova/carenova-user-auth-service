namespace UserAuth.Api.DTOs.Profile;

public class UpdateProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string? LandPhoneNumber { get; set; }
    public string Address { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
}
