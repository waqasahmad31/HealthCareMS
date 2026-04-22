namespace HealthCareMS.Application.Auth;

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    AuthenticatedUser User);

public sealed record AuthenticatedUser(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    Guid? TenantId,
    IReadOnlyCollection<string> Permissions);
