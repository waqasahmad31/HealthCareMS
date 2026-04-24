using HealthCareMS.Application.Security;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace HealthCareMS.Infrastructure.Security;

public sealed class SecurityCenterService(HealthCareDbContext dbContext) : ISecurityCenterService
{
    private const string Issuer = "HealthCareMS";

    public async Task<Result<SecurityOverviewResponse>> GetOverviewAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return Result<SecurityOverviewResponse>.Failure(new Error("SECURITY_USER_NOT_FOUND", "User was not found."));
        }

        var activeSessions = await dbContext.Set<UserAuthSession>()
            .CountAsync(x => x.UserId == userId && x.RevokedAt == null && x.ExpiresAt > DateTimeOffset.UtcNow, cancellationToken);
        var failedLogins = await dbContext.Set<UserLoginActivity>()
            .CountAsync(x => x.UserId == userId && !x.IsSuccessful, cancellationToken);

        return Result<SecurityOverviewResponse>.Success(new SecurityOverviewResponse(
            userId,
            user.TwoFactorEnabled,
            activeSessions,
            failedLogins,
            user.LastLoginAt));
    }

    public async Task<Result<IReadOnlyList<UserAuthSessionResponse>>> GetActiveSessionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var sessions = await dbContext.Set<UserAuthSession>()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.LastSeenAt ?? x.IssuedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<UserAuthSessionResponse>>.Success(sessions.Select(Map).ToList());
    }

    public async Task<Result<IReadOnlyList<LoginActivityResponse>>> GetLoginHistoryAsync(Guid userId, CancellationToken cancellationToken)
    {
        var email = await dbContext.Users
            .Where(x => x.Id == userId)
            .Select(x => x.Email)
            .SingleOrDefaultAsync(cancellationToken);
        var activities = await dbContext.Set<UserLoginActivity>()
            .Where(x => x.UserId == userId || (x.UserId == null && x.Email == email))
            .OrderByDescending(x => x.AttemptedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<LoginActivityResponse>>.Success(activities.Select(Map).ToList());
    }

    public async Task<Result<UserAuthSessionResponse>> RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await dbContext.Set<UserAuthSession>()
            .SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, cancellationToken);
        if (session is null)
        {
            return Result<UserAuthSessionResponse>.Failure(new Error("SECURITY_SESSION_NOT_FOUND", "Session was not found."));
        }

        session.RevokedAt ??= DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<UserAuthSessionResponse>.Success(Map(session));
    }

    public async Task<Result<TwoFactorSetupResponse>> BeginTwoFactorSetupAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return Result<TwoFactorSetupResponse>.Failure(new Error("SECURITY_USER_NOT_FOUND", "User was not found."));
        }

        user.TwoFactorSecret = TotpUtility.GenerateSecret();
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<TwoFactorSetupResponse>.Success(MapTwoFactor(user));
    }

    public async Task<Result<TwoFactorSetupResponse>> EnableTwoFactorAsync(Guid userId, EnableTwoFactorRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return Result<TwoFactorSetupResponse>.Failure(new Error("SECURITY_USER_NOT_FOUND", "User was not found."));
        }

        if (string.IsNullOrWhiteSpace(user.TwoFactorSecret))
        {
            user.TwoFactorSecret = TotpUtility.GenerateSecret();
        }

        if (!TotpUtility.ValidateCode(user.TwoFactorSecret, request.VerificationCode))
        {
            return Result<TwoFactorSetupResponse>.Failure(new Error("SECURITY_2FA_CODE_INVALID", "Verification code is invalid."));
        }

        user.TwoFactorEnabled = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<TwoFactorSetupResponse>.Success(MapTwoFactor(user));
    }

    public async Task<Result<SecurityOverviewResponse>> DisableTwoFactorAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return Result<SecurityOverviewResponse>.Failure(new Error("SECURITY_USER_NOT_FOUND", "User was not found."));
        }

        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetOverviewAsync(userId, cancellationToken);
    }

    private static UserAuthSessionResponse Map(UserAuthSession session)
    {
        return new UserAuthSessionResponse(
            session.Id,
            session.UserId,
            session.IssuedAt,
            session.ExpiresAt,
            session.RevokedAt,
            session.LastSeenAt,
            session.IpAddress,
            session.UserAgent,
            session.DeviceLabel);
    }

    private static LoginActivityResponse Map(UserLoginActivity activity)
    {
        return new LoginActivityResponse(
            activity.Id,
            activity.UserId,
            activity.Email,
            activity.IsSuccessful,
            activity.AttemptedAt,
            activity.IpAddress,
            activity.UserAgent,
            activity.FailureReason);
    }

    private static TwoFactorSetupResponse MapTwoFactor(Domain.Identity.ApplicationUser user)
    {
        var secret = user.TwoFactorSecret ?? string.Empty;
        return new TwoFactorSetupResponse(
            user.Id,
            user.TwoFactorEnabled,
            secret,
            secret,
            TotpUtility.BuildOtpAuthUri(Issuer, user.Email, secret));
    }
}
