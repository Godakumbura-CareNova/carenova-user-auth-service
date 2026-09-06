using UserAuth.Api.DTOs.Auth;
using UserAuth.Api.DTOs.Profile;
using UserAuth.Api.DTOs.User;

namespace UserAuth.Api.Services.Interfaces;

public interface IUserService
{
    Task<UserResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<ProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ProfileDto?> UpdateProfileAsync(Guid userId, UpdateProfileDto request, CancellationToken cancellationToken = default);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserDto?> UpdateUserRoleAsync(Guid userId, UpdateRoleDto request, CancellationToken cancellationToken = default);
    Task<UserDto?> UpdateAccountStatusAsync(Guid userId, UpdateAccountStatusDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
