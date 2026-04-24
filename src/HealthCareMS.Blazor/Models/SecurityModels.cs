namespace HealthCareMS.Blazor.Models;

public sealed record SecurityOverviewModel(
    Guid UserId,
    bool TwoFactorEnabled,
    int ActiveSessionCount,
    int FailedLoginCount,
    DateTimeOffset? LastLoginAt);

public sealed record UserAuthSessionModel(
    Guid Id,
    Guid UserId,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt,
    DateTimeOffset? LastSeenAt,
    string? IpAddress,
    string? UserAgent,
    string? DeviceLabel);

public sealed record LoginActivityModel(
    Guid Id,
    Guid? UserId,
    string Email,
    bool IsSuccessful,
    DateTimeOffset AttemptedAt,
    string? IpAddress,
    string? UserAgent,
    string? FailureReason);

public sealed record TwoFactorSetupModel(
    Guid UserId,
    bool TwoFactorEnabled,
    string SecretKey,
    string ManualEntryCode,
    string OtpAuthUri);

public sealed class EnableTwoFactorFormModel
{
    public string VerificationCode { get; set; } = string.Empty;
}
