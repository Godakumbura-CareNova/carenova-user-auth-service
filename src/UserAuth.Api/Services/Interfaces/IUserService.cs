using UserAuth.Api.DTOs.Auth;
using UserAuth.Api.DTOs.User;

namespace UserAuth.Api.Services.Interfaces;

public interface IUserService
{
    Task<UserResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> GetAllUsersAsync(string? search, CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserDto?> UpdateUserRoleAsync(Guid userId, UpdateRoleDto request, CancellationToken cancellationToken = default);
    Task<UserDto?> UpdateAccountStatusAsync(Guid userId, UpdateAccountStatusDto request, CancellationToken cancellationToken = default);
}
