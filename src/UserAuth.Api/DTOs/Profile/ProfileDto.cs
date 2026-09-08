namespace UserAuth.Api.DTOs.Profile;

public class ProfileDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? MobileNumber { get; set; }
    public string? LandPhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public bool HasProfilePicture { get; set; }
    public string? ProfilePictureUrl { get; set; }
}
