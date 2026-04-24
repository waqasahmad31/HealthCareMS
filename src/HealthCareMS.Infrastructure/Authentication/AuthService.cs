using System.Security.Cryptography;
using System.Text;
using HealthCareMS.Application.Abstractions.Authentication;
using HealthCareMS.Application.Auth;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Infrastructure.Security;
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
            await RecordLoginAttemptAsync(user?.Id, email, false, "Invalid credentials", cancellationToken);
            return Result<LoginResponse>.Failure(new Error("AUTH_INVALID_CREDENTIALS", "Invalid email or password."));
        }

        if (!user.IsActive)
        {
            await RecordLoginAttemptAsync(user.Id, email, false, "Account disabled", cancellationToken);
            return Result<LoginResponse>.Failure(new Error("AUTH_ACCOUNT_INACTIVE", "Account is disabled."));
        }

        if (user.LockoutUntil.HasValue && user.LockoutUntil.Value > DateTimeOffset.UtcNow)
        {
            await RecordLoginAttemptAsync(user.Id, email, false, "Account locked", cancellationToken);
            return Result<LoginResponse>.Failure(new Error("AUTH_ACCOUNT_LOCKED", "Account is temporarily locked."));
        }

        if (!user.IsEmailVerified)
        {
            await RecordLoginAttemptAsync(user.Id, email, false, "Email not verified", cancellationToken);
            return Result<LoginResponse>.Failure(new Error("AUTH_EMAIL_NOT_VERIFIED", "Email is not verified."));
        }

        if (user.TwoFactorEnabled && !TotpUtility.ValidateCode(user.TwoFactorSecret ?? string.Empty, request.TwoFactorCode ?? string.Empty))
        {
            await RecordLoginAttemptAsync(user.Id, email, false, "Two-factor validation failed", cancellationToken);
            return Result<LoginResponse>.Failure(new Error("AUTH_2FA_INVALID", "Two-factor authentication code is invalid."));
        }

        var permissions = ResolvePermissions(user);
        var accessToken = jwtTokenService.CreateAccessToken(user, permissions);
        var refreshToken = CreateRefreshToken();
        var refreshTokenHash = HashToken(refreshToken);
        var now = DateTimeOffset.UtcNow;

        user.LastLoginAt = now;
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = now.AddDays(7);
        user.FailedLoginCount = 0;

        dbContext.UserAuthSessions.Add(new UserAuthSession
        {
            UserId = user.Id,
            RefreshTokenHash = refreshTokenHash,
            IssuedAt = now,
            ExpiresAt = user.RefreshTokenExpiry.Value,
            LastSeenAt = now,
            IpAddress = null,
            UserAgent = null,
            DeviceLabel = "Web Session"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await RecordLoginAttemptAsync(user.Id, email, true, failureReason: null, cancellationToken);

        var response = new LoginResponse(
            accessToken.AccessToken,
            refreshToken,
            accessToken.ExpiresAt,
            new AuthenticatedUser(user.Id, user.FullName, user.Email, user.Role.Name, user.TenantId, permissions));

        return Result<LoginResponse>.Success(response);
    }

    public async Task<Result<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Result<LoginResponse>.Failure(new Error("AUTH_REFRESH_TOKEN_INVALID", "Refresh token is required."));
        }

        var session = await dbContext.UserAuthSessions
            .SingleOrDefaultAsync(x => x.RefreshTokenHash == HashToken(request.RefreshToken) && x.RevokedAt == null, cancellationToken);
        if (session is null || session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return Result<LoginResponse>.Failure(new Error("AUTH_REFRESH_TOKEN_INVALID", "Refresh token is invalid or expired."));
        }

        var user = await dbContext.Users
            .Include(x => x.Role)
            .ThenInclude(x => x.RolePermissions)
            .ThenInclude(x => x.Permission)
            .Include(x => x.UserPermissions)
            .ThenInclude(x => x.Permission)
            .FirstOrDefaultAsync(x => x.RefreshToken == request.RefreshToken, cancellationToken);

        if (user is null || user.RefreshTokenExpiry is null || user.RefreshTokenExpiry <= DateTimeOffset.UtcNow)
        {
            return Result<LoginResponse>.Failure(new Error("AUTH_REFRESH_TOKEN_INVALID", "Refresh token is invalid or expired."));
        }

        if (!user.IsActive)
        {
            return Result<LoginResponse>.Failure(new Error("AUTH_ACCOUNT_INACTIVE", "Account is disabled."));
        }

        var permissions = ResolvePermissions(user);
        var accessToken = jwtTokenService.CreateAccessToken(user, permissions);
        var refreshToken = CreateRefreshToken();
        var now = DateTimeOffset.UtcNow;

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = now.AddDays(7);
        session.RefreshTokenHash = HashToken(refreshToken);
        session.ExpiresAt = user.RefreshTokenExpiry.Value;
        session.LastSeenAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse(
            accessToken.AccessToken,
            refreshToken,
            accessToken.ExpiresAt,
            new AuthenticatedUser(user.Id, user.FullName, user.Email, user.Role.Name, user.TenantId, permissions)));
    }

    public async Task<Result> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            return Result.Failure(new Error("AUTH_USER_INVALID", "User is required."));
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(new Error("AUTH_USER_NOT_FOUND", "User was not found."));
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;

        var sessions = await dbContext.UserAuthSessions
            .Where(x => x.UserId == request.UserId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.RevokedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
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

    private static string HashToken(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private async Task RecordLoginAttemptAsync(
        Guid? userId,
        string email,
        bool isSuccessful,
        string? failureReason,
        CancellationToken cancellationToken)
    {
        dbContext.UserLoginActivities.Add(new UserLoginActivity
        {
            UserId = userId,
            Email = email,
            IsSuccessful = isSuccessful,
            AttemptedAt = DateTimeOffset.UtcNow,
            IpAddress = null,
            UserAgent = null,
            FailureReason = failureReason
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
