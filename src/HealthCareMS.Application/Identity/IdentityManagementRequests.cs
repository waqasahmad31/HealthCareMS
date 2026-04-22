namespace HealthCareMS.Application.Identity;

public sealed record CreateRoleRequest(
    Guid? TenantId,
    string Name,
    IReadOnlyList<string> PermissionKeys);

public sealed record CreateUserRequest(
    Guid? TenantId,
    Guid RoleId,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string Password,
    bool IsEmailVerified = true);

public sealed record GrantPermissionsRequest(
    string TargetType,
    Guid TargetId,
    IReadOnlyList<string> PermissionKeys,
    bool IsGranted = true);
