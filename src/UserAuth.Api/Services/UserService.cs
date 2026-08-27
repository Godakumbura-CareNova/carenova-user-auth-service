using Microsoft.AspNetCore.Identity;
using UserAuth.Api.DTOs.Auth;
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
            Role = "User",
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
}
