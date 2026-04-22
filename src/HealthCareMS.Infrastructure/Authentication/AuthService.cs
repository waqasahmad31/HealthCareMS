using System.Security.Cryptography;
using HealthCareMS.Application.Abstractions.Authentication;
using HealthCareMS.Application.Auth;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace HealthCareMS.Infrastructure.Authentication;

public sealed class AuthService(
    HealthCareDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService) : IAuthService
{
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await dbContext.Users
            .Include(x => x.Role)
            .ThenInclude(x => x.RolePermissions)
            .ThenInclude(x => x.Permission)
            .Include(x => x.UserPermissions)
            .ThenInclude(x => x.Permission)
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result<LoginResponse>.Failure(new Error("AUTH_INVALID_CREDENTIALS", "Invalid email or password."));
        }

        if (!user.IsActive)
        {
            return Result<LoginResponse>.Failure(new Error("AUTH_ACCOUNT_INACTIVE", "Account is disabled."));
        }

        if (user.LockoutUntil.HasValue && user.LockoutUntil.Value > DateTimeOffset.UtcNow)
        {
            return Result<LoginResponse>.Failure(new Error("AUTH_ACCOUNT_LOCKED", "Account is temporarily locked."));
        }

        if (!user.IsEmailVerified)
        {
            return Result<LoginResponse>.Failure(new Error("AUTH_EMAIL_NOT_VERIFIED", "Email is not verified."));
        }

        var permissions = ResolvePermissions(user);
        var accessToken = jwtTokenService.CreateAccessToken(user, permissions);
        var refreshToken = CreateRefreshToken();

        user.LastLoginAt = DateTimeOffset.UtcNow;
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTimeOffset.UtcNow.AddDays(7);
        user.FailedLoginCount = 0;

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new LoginResponse(
            accessToken.AccessToken,
            refreshToken,
            accessToken.ExpiresAt,
            new AuthenticatedUser(user.Id, user.FullName, user.Email, user.Role.Name, user.TenantId, permissions));

        return Result<LoginResponse>.Success(response);
    }

    private static IReadOnlyCollection<string> ResolvePermissions(ApplicationUser user)
    {
        var denied = user.UserPermissions
            .Where(x => !x.IsGranted)
            .Select(x => x.Permission.PermissionKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var permissions = user.Role.RolePermissions
            .Select(x => x.Permission.PermissionKey)
            .Where(x => !denied.Contains(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var directGrant in user.UserPermissions.Where(x => x.IsGranted))
        {
            permissions.Add(directGrant.Permission.PermissionKey);
        }

        return permissions.OrderBy(x => x).ToArray();
    }

    private static string CreateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}
