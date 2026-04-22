using HealthCareMS.Application.Abstractions.Authentication;
using HealthCareMS.Application.Abstractions.Tenancy;
using HealthCareMS.Application.Auth;
using HealthCareMS.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCareMS.API.Controllers;

[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService, ICurrentUser currentUser) : ApiControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var validationErrors = new List<ValidationError>();
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            validationErrors.Add(new ValidationError(nameof(request.Email), "Email is required."));
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            validationErrors.Add(new ValidationError(nameof(request.Password), "Password is required."));
        }

        if (validationErrors.Count > 0)
        {
            return Fail(Error.Validation(validationErrors));
        }

        var result = await authService.LoginAsync(request, cancellationToken);
        return FromResult(result);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Fail(Error.Validation([new ValidationError(nameof(request.RefreshToken), "RefreshToken is required.")]));
        }

        var result = await authService.RefreshTokenAsync(request, cancellationToken);
        return FromResult(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
        {
            return Fail(new Error("AUTH_USER_INVALID", "Authenticated user is required."));
        }

        var result = await authService.LogoutAsync(new LogoutRequest(currentUser.UserId.Value), cancellationToken);
        return FromResult(result);
    }
}
