using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using UserAuth.Api.DTOs.Profile;
using UserAuth.Api.Models;
using UserAuth.Api.Repositories;
using UserAuth.Api.Services.Interfaces;

namespace UserAuth.Api.Services;

public class ProfileService : IProfileService
{
    private const string ProfilePictureDirectory = "uploads/profile-pictures";

    private readonly IUserRepository _userRepository;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public ProfileService(IUserRepository userRepository, IWebHostEnvironment webHostEnvironment)
    {
        _userRepository = userRepository;
        _webHostEnvironment = webHostEnvironment;
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

    public async Task<ProfileDto?> UpdateProfilePictureAsync(Guid userId, Stream content, string fileExtension, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var webRootPath = _webHostEnvironment.WebRootPath ?? Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot");
        var uploadsDirectory = Path.Combine(webRootPath, ProfilePictureDirectory.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(uploadsDirectory);

        var fileName = $"{userId}{fileExtension}";
        var filePath = Path.Combine(uploadsDirectory, fileName);

        if (!string.IsNullOrWhiteSpace(user.ProfilePicturePath))
        {
            var existingFileName = Path.GetFileName(user.ProfilePicturePath);
            if (!string.Equals(existingFileName, fileName, StringComparison.OrdinalIgnoreCase))
            {
                var existingFilePath = Path.Combine(uploadsDirectory, existingFileName);
                if (File.Exists(existingFilePath))
                {
                    File.Delete(existingFilePath);
                }
            }
        }

        await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        user.ProfilePicturePath = $"/{ProfilePictureDirectory}/{fileName}";
        return ToProfileDto(await _userRepository.UpdateAsync(user, cancellationToken));
    }

    private static ProfileDto ToProfileDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FullName = user.FullName,
        MobileNumber = AsOptional(user.MobileNumber),
        LandPhoneNumber = AsOptional(user.LandPhoneNumber),
        Address = AsOptional(user.Address),
        DateOfBirth = user.DateOfBirth == default ? null : user.DateOfBirth,
        Gender = AsOptional(user.Gender),
        HasProfilePicture = !string.IsNullOrWhiteSpace(user.ProfilePicturePath),
        ProfilePictureUrl = user.ProfilePicturePath
    };

    private static string? AsOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
