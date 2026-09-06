using Microsoft.AspNetCore.Identity;
using UserAuth.Api.DTOs.Auth;
using UserAuth.Api.DTOs.Profile;
using UserAuth.Api.DTOs.User;
using UserAuth.Api.Exceptions;
using UserAuth.Api.Models;
using UserAuth.Api.Repositories;
using UserAuth.Api.Services.Interfaces;

namespace UserAuth.Api.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserService(IUserRepository userRepository, IPasswordHasher<User>? passwordHasher = null)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher ?? new PasswordHasher<User>();
    }

    public async Task<UserResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim();

        if (await _userRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new DuplicateEmailException($"A user with email '{normalizedEmail}' already exists.");
        }

        var fullName = request.FullName?.Trim();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            fullName = string.Join(" ", new[] { request.FirstName?.Trim(), request.LastName?.Trim() }
                .Where(part => !string.IsNullOrWhiteSpace(part)));
        }

        var user = new User
        {
            FullName = fullName,
            Email = normalizedEmail,
            MobileNumber = request.MobileNumber.Trim(),
            LandPhoneNumber = request.LandPhoneNumber.Trim(),
            Address = request.Address.Trim(),
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender.Trim(),
            Role = RoleNames.Patient,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            PasswordHash = _passwordHasher.HashPassword(new User(), request.Password.Trim())
        };

        var createdUser = await _userRepository.AddAsync(user, cancellationToken);

        return new UserResponseDto
        {
            Id = createdUser.Id,
            FullName = createdUser.FullName,
            Email = createdUser.Email,
            MobileNumber = createdUser.MobileNumber,
            LandPhoneNumber = createdUser.LandPhoneNumber,
            Address = createdUser.Address,
            DateOfBirth = createdUser.DateOfBirth,
            Gender = createdUser.Gender,
            Role = createdUser.Role,
            IsActive = createdUser.IsActive,
            CreatedAt = createdUser.CreatedAt
        };
    }

    public async Task<ProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user is null ? null : ToProfileDto(user);
    }

    public async Task<ProfileDto?> UpdateProfileAsync(Guid userId, UpdateProfileDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.FullName = request.FullName.Trim();
        user.MobileNumber = request.MobileNumber.Trim();
        user.LandPhoneNumber = request.LandPhoneNumber?.Trim() ?? string.Empty;
        user.Address = request.Address.Trim();
        user.DateOfBirth = request.DateOfBirth;
        user.Gender = request.Gender.Trim();

        return ToProfileDto(await _userRepository.UpdateAsync(user, cancellationToken));
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return false;
        }

        var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword.Trim());
        if (verify == PasswordVerificationResult.Failed)
        {
            return false;
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword.Trim());
        await _userRepository.UpdateAsync(user, cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users.Select(ToUserDto).ToList();
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user is null ? null : ToUserDto(user);
    }

    public async Task<UserDto?> UpdateUserRoleAsync(Guid userId, UpdateRoleDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        if (!RoleNames.TryNormalize(request.Role, out var normalizedRole))
        {
            throw new ArgumentException("Unsupported role.");
        }

        user.Role = normalizedRole;
        var updated = await _userRepository.UpdateAsync(user, cancellationToken);
        return ToUserDto(updated);
    }

    public async Task<UserDto?> UpdateAccountStatusAsync(Guid userId, UpdateAccountStatusDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.IsActive = request.IsActive;
        var updated = await _userRepository.UpdateAsync(user, cancellationToken);
        return ToUserDto(updated);
    }

    public async Task<bool> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        return await _userRepository.DeleteAsync(user, cancellationToken);
    }

    private static UserDto ToUserDto(User user)
    {
        var role = RoleNames.TryNormalize(user.Role, out var normalizedRole)
            ? normalizedRole
            : RoleNames.Patient;

        return new UserDto
        {
            Id = user.Id.ToString(),
            Email = user.Email,
            Role = role,
            IsActive = user.IsActive
        };
    }

    private static ProfileDto ToProfileDto(User user)
    {
        return new ProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            MobileNumber = AsOptional(user.MobileNumber),
            LandPhoneNumber = AsOptional(user.LandPhoneNumber),
            Address = AsOptional(user.Address),
            DateOfBirth = user.DateOfBirth == default ? null : user.DateOfBirth,
            Gender = AsOptional(user.Gender),
            HasProfilePicture = !string.IsNullOrWhiteSpace(user.ProfilePicturePath),
            ProfilePictureUrl = user.ProfilePicturePath
        };
    }

    private static string? AsOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
