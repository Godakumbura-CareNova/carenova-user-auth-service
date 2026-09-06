using Microsoft.AspNetCore.Identity;
using UserAuth.Api.DTOs.Profile;
using UserAuth.Api.Models;
using UserAuth.Api.Repositories;
using UserAuth.Api.Services.Interfaces;

namespace UserAuth.Api.Services;

public class ProfileService : IProfileService
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public ProfileService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ProfileDto?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user is null ? null : ToProfileDto(user);
    }

    public async Task<ProfileDto?> UpdateAsync(Guid userId, UpdateProfileDto request, CancellationToken cancellationToken = default)
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
        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash)
            || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword.Trim()) == PasswordVerificationResult.Failed)
        {
            return false;
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword.Trim());
        await _userRepository.UpdateAsync(user, cancellationToken);
        return true;
    }

    private static ProfileDto ToProfileDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FullName = user.FullName,
        MobileNumber = user.MobileNumber,
        LandPhoneNumber = user.LandPhoneNumber,
        Address = user.Address,
        DateOfBirth = user.DateOfBirth,
        Gender = user.Gender
    };
}