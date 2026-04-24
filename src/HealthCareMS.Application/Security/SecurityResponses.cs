namespace HealthCareMS.Application.Security;

public sealed record SecurityOverviewResponse(
    Guid UserId,
    bool TwoFactorEnabled,
    int ActiveSessionCount,
    int FailedLoginCount,
    DateTimeOffset? LastLoginAt);

public sealed record UserAuthSessionResponse(
    Guid Id,
    Guid UserId,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt,
    DateTimeOffset? LastSeenAt,
    string? IpAddress,
    string? UserAgent,
    string? DeviceLabel);

public sealed record LoginActivityResponse(
    Guid Id,
    Guid? UserId,
    string Email,
    bool IsSuccessful,
    DateTimeOffset AttemptedAt,
    string? IpAddress,
    string? UserAgent,
    string? FailureReason);

public sealed record TwoFactorSetupResponse(
    Guid UserId,
    bool TwoFactorEnabled,
    string SecretKey,
    string ManualEntryCode,
    string OtpAuthUri);
