using UserAuth.Api.DTOs.Profile;

namespace UserAuth.Api.Services.Interfaces;

public interface IProfileService
{
    Task<ProfileDto?> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ProfileDto?> UpdateAsync(Guid userId, UpdateProfileDto request, CancellationToken cancellationToken = default);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto request, CancellationToken cancellationToken = default);
    Task<ProfileDto?> UpdateProfilePictureAsync(Guid userId, Stream content, string fileExtension, CancellationToken cancellationToken = default);
}
