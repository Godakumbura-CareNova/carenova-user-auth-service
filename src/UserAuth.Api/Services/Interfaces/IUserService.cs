using UserAuth.Api.DTOs.Auth;
using UserAuth.Api.DTOs.User;

namespace UserAuth.Api.Services.Interfaces;

public interface IUserService
{
    Task<UserResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
}
