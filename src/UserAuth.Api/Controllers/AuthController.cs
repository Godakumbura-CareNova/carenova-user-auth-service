using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using UserAuth.Api.DTOs.Auth;
using UserAuth.Api.Exceptions;
using UserAuth.Api.Services.Interfaces;

namespace UserAuth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequestDto> _registerValidator;
    private readonly IValidator<LoginDto> _loginValidator;

    public AuthController(
        IUserService userService,
        IAuthService authService,
        IValidator<RegisterRequestDto> registerValidator,
        IValidator<LoginDto> loginValidator)
    {
        _userService = userService;
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        try
        {
            var createdUser = await _userService.RegisterAsync(request, cancellationToken);
            return CreatedAtAction(nameof(Register), new { id = createdUser.Id }, createdUser);
        }
        catch (DuplicateEmailException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto request, CancellationToken cancellationToken)
    {
        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return ValidationProblem(ModelState);
        }

        var response = await _authService.LoginAsync(request, cancellationToken);
        return response is null
            ? Unauthorized(new { message = "Invalid email or password." })
            : Ok(response);
    }
}
