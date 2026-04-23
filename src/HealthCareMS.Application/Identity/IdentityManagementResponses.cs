namespace HealthCareMS.Application.Identity;

public sealed record PermissionResponse(
    Guid Id,
    string PermissionKey,
    string Module,
    string Action,
    string Description);

public sealed record RoleResponse(
    Guid Id,
    Guid? TenantId,
    string Name,
    bool IsSystemRole,
    IReadOnlyList<string> Permissions);

public sealed record UserResponse(
    Guid Id,
    Guid? TenantId,
    Guid RoleId,
    string Role,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string? PhoneNumber,
    bool IsActive,
    bool IsEmailVerified,
    IReadOnlyList<string> Permissions);

public sealed record ManageableUserResponse(
    Guid Id,
    Guid? TenantId,
    Guid RoleId,
    string Role,
    string FullName,
    string Email,
    string? PhoneNumber,
    Guid? CreatedByUserId,
    bool IsActive,
    bool IsEmailVerified);

public sealed record PermissionGrantResponse(
    string TargetType,
    Guid TargetId,
    IReadOnlyList<string> GrantedPermissions,
    IReadOnlyList<string> DeniedPermissions);

public sealed record UserMenuAssignmentResponse(
    Guid UserId,
    IReadOnlyList<string> MenuItemKeys,
    DateTimeOffset UpdatedAt);
