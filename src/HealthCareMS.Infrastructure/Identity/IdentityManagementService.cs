using HealthCareMS.Application.Abstractions.Authentication;
using HealthCareMS.Application.Abstractions.Tenancy;
using HealthCareMS.Application.Identity;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Shared.Common;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HealthCareMS.Infrastructure.Identity;

public sealed class IdentityManagementService(
    HealthCareDbContext dbContext,
    ICurrentUser currentUser,
    IPasswordHasher passwordHasher) : IIdentityManagementService
{
    private const string NavigationSettingKey = "Platform.Navigation.MenuConfigJson";
    private const string NavigationAssignmentSettingPrefix = "Navigation.Assignment.User.";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public async Task<IReadOnlyList<PermissionResponse>> GetPermissionsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Permissions
            .OrderBy(x => x.Module)
            .ThenBy(x => x.PermissionKey)
            .Select(x => new PermissionResponse(x.Id, x.PermissionKey, x.Module, x.Action, x.Description))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<RoleResponse>> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var validationErrors = ValidateCreateRole(request);
        if (validationErrors.Count > 0)
        {
            return Result<RoleResponse>.Failure(Error.Validation(validationErrors));
        }

        var permissionCheck = await ValidatePermissionKeysAndCeilingAsync(request.PermissionKeys, cancellationToken);
        if (permissionCheck.IsFailure)
        {
            return Result<RoleResponse>.Failure(permissionCheck.Error);
        }

        var tenantIdResult = await ResolveTenantIdAsync(request.TenantId, cancellationToken);
        if (tenantIdResult.IsFailure)
        {
            return Result<RoleResponse>.Failure(tenantIdResult.Error);
        }

        var roleName = request.Name.Trim();
        var roleExists = await dbContext.Roles.AnyAsync(x => x.TenantId == tenantIdResult.Value && x.Name == roleName, cancellationToken);
        if (roleExists)
        {
            return Result<RoleResponse>.Failure(new Error("ROLE_NAME_EXISTS", "A role with this name already exists for this scope."));
        }

        var permissions = await dbContext.Permissions
            .Where(x => request.PermissionKeys.Contains(x.PermissionKey))
            .ToListAsync(cancellationToken);

        var role = new Role
        {
            TenantId = tenantIdResult.Value,
            Name = roleName,
            IsSystemRole = currentUser.IsSuperAdmin && tenantIdResult.Value is null
        };

        foreach (var permission in permissions)
        {
            role.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id,
                Permission = permission
            });
        }

        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<RoleResponse>.Success(Map(role));
    }

    public async Task<Result<UserResponse>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var validationErrors = ValidateCreateUser(request);
        if (validationErrors.Count > 0)
        {
            return Result<UserResponse>.Failure(Error.Validation(validationErrors));
        }

        var role = await dbContext.Roles
            .Include(x => x.RolePermissions)
            .ThenInclude(x => x.Permission)
            .SingleOrDefaultAsync(x => x.Id == request.RoleId, cancellationToken);

        if (role is null)
        {
            return Result<UserResponse>.Failure(new Error("ROLE_NOT_FOUND", "Role was not found."));
        }

        var tenantIdResult = await ResolveTenantIdAsync(request.TenantId, cancellationToken);
        if (tenantIdResult.IsFailure)
        {
            return Result<UserResponse>.Failure(tenantIdResult.Error);
        }

        if (role.TenantId != tenantIdResult.Value && role.TenantId is not null)
        {
            return Result<UserResponse>.Failure(new Error("ROLE_TENANT_MISMATCH", "Role does not belong to the requested tenant."));
        }

        var rolePermissionKeys = role.RolePermissions.Select(x => x.Permission.PermissionKey).ToArray();
        var permissionCheck = await ValidatePermissionKeysAndCeilingAsync(rolePermissionKeys, cancellationToken);
        if (permissionCheck.IsFailure)
        {
            return Result<UserResponse>.Failure(permissionCheck.Error);
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var emailExists = await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken);
        if (emailExists)
        {
            return Result<UserResponse>.Failure(new Error("USER_EMAIL_EXISTS", "A user with this email already exists."));
        }

        var user = new ApplicationUser
        {
            TenantId = tenantIdResult.Value,
            RoleId = request.RoleId,
            Role = role,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PhoneNumber = Normalize(request.PhoneNumber),
            PasswordHash = passwordHasher.Hash(request.Password),
            IsActive = true,
            IsEmailVerified = request.IsEmailVerified,
            CreatedByUserId = currentUser.UserId
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<UserResponse>.Success(Map(user));
    }

    public async Task<Result<PermissionGrantResponse>> GrantPermissionsAsync(
        GrantPermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateGrantPermissions(request);
        if (validationErrors.Count > 0)
        {
            return Result<PermissionGrantResponse>.Failure(Error.Validation(validationErrors));
        }

        var permissionCheck = await ValidatePermissionKeysAndCeilingAsync(request.PermissionKeys, cancellationToken);
        if (permissionCheck.IsFailure)
        {
            return Result<PermissionGrantResponse>.Failure(permissionCheck.Error);
        }

        var permissions = await dbContext.Permissions
            .Where(x => request.PermissionKeys.Contains(x.PermissionKey))
            .ToListAsync(cancellationToken);

        if (string.Equals(request.TargetType, "Role", StringComparison.OrdinalIgnoreCase))
        {
            return await GrantRolePermissionsAsync(request, permissions, cancellationToken);
        }

        if (string.Equals(request.TargetType, "User", StringComparison.OrdinalIgnoreCase))
        {
            return await GrantUserPermissionsAsync(request, permissions, cancellationToken);
        }

        return Result<PermissionGrantResponse>.Failure(new Error("PERMISSION_TARGET_INVALID", "TargetType must be Role or User."));
    }

    public async Task<Result<UserMenuAssignmentResponse>> GetUserMenuAssignmentAsync(Guid userId, CancellationToken cancellationToken)
    {
        var targetUser = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (targetUser is null)
        {
            return Result<UserMenuAssignmentResponse>.Failure(new Error("USER_NOT_FOUND", "User was not found."));
        }

        if (!CanManageUserMenus(targetUser))
        {
            return Result<UserMenuAssignmentResponse>.Failure(new Error("USER_MENU_FORBIDDEN", "You cannot view this user's menu assignment."));
        }

        var assignment = await ReadUserAssignmentAsync(userId, cancellationToken);
        return Result<UserMenuAssignmentResponse>.Success(new UserMenuAssignmentResponse(
            userId,
            assignment.MenuItemKeys,
            assignment.UpdatedAt));
    }

    public async Task<Result<UserMenuAssignmentResponse>> AssignUserMenuAsync(
        Guid userId,
        AssignUserMenuRequest request,
        CancellationToken cancellationToken)
    {
        var targetUser = await dbContext.Users
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (targetUser is null)
        {
            return Result<UserMenuAssignmentResponse>.Failure(new Error("USER_NOT_FOUND", "User was not found."));
        }

        if (!CanManageUserMenus(targetUser))
        {
            return Result<UserMenuAssignmentResponse>.Failure(new Error("USER_MENU_FORBIDDEN", "You cannot assign menus for this user."));
        }

        var menuItemKeys = (request.MenuItemKeys ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var assignableKeysResult = await GetAssignableMenuKeysForCurrentUserAsync(cancellationToken);
        if (assignableKeysResult.IsFailure)
        {
            return Result<UserMenuAssignmentResponse>.Failure(assignableKeysResult.Error);
        }

        var invalidKeys = menuItemKeys
            .Where(x => !assignableKeysResult.Value.Contains(x))
            .ToArray();
        if (invalidKeys.Length > 0)
        {
            return Result<UserMenuAssignmentResponse>.Failure(new Error(
                "USER_MENU_CEILING_EXCEEDED",
                $"Cannot assign menus outside your ceiling: {string.Join(", ", invalidKeys)}."));
        }

        await UpsertUserAssignmentAsync(userId, menuItemKeys, cancellationToken);
        var saved = await ReadUserAssignmentAsync(userId, cancellationToken);
        return Result<UserMenuAssignmentResponse>.Success(new UserMenuAssignmentResponse(
            userId,
            saved.MenuItemKeys,
            saved.UpdatedAt));
    }

    private async Task<Result<PermissionGrantResponse>> GrantRolePermissionsAsync(
        GrantPermissionsRequest request,
        IReadOnlyList<Permission> permissions,
        CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles
            .Include(x => x.RolePermissions)
            .ThenInclude(x => x.Permission)
            .SingleOrDefaultAsync(x => x.Id == request.TargetId, cancellationToken);

        if (role is null)
        {
            return Result<PermissionGrantResponse>.Failure(new Error("ROLE_NOT_FOUND", "Role was not found."));
        }

        if (!CanAccessTenant(role.TenantId))
        {
            return Result<PermissionGrantResponse>.Failure(new Error("TENANT_FORBIDDEN", "You cannot manage roles outside your tenant."));
        }

        if (role.IsSystemRole && !currentUser.IsSuperAdmin)
        {
            return Result<PermissionGrantResponse>.Failure(new Error("ROLE_SYSTEM_FORBIDDEN", "System roles can only be managed by Super Admin."));
        }

        foreach (var permission in permissions)
        {
            var existing = role.RolePermissions.FirstOrDefault(x => x.PermissionId == permission.Id);
            if (request.IsGranted && existing is null)
            {
                role.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permission.Id,
                    Permission = permission
                });
            }

            if (!request.IsGranted && existing is not null)
            {
                role.RolePermissions.Remove(existing);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<PermissionGrantResponse>.Success(ToGrantResponse(
            "Role",
            role.Id,
            role.RolePermissions.Select(x => x.Permission.PermissionKey),
            []));
    }

    private async Task<Result<PermissionGrantResponse>> GrantUserPermissionsAsync(
        GrantPermissionsRequest request,
        IReadOnlyList<Permission> permissions,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(x => x.UserPermissions)
            .ThenInclude(x => x.Permission)
            .SingleOrDefaultAsync(x => x.Id == request.TargetId, cancellationToken);

        if (user is null)
        {
            return Result<PermissionGrantResponse>.Failure(new Error("USER_NOT_FOUND", "User was not found."));
        }

        if (!CanAccessTenant(user.TenantId))
        {
            return Result<PermissionGrantResponse>.Failure(new Error("TENANT_FORBIDDEN", "You cannot manage users outside your tenant."));
        }

        foreach (var permission in permissions)
        {
            var existing = user.UserPermissions.FirstOrDefault(x => x.PermissionId == permission.Id);
            if (existing is null)
            {
                user.UserPermissions.Add(new UserPermission
                {
                    UserId = user.Id,
                    PermissionId = permission.Id,
                    Permission = permission,
                    IsGranted = request.IsGranted,
                    GrantedByUserId = currentUser.UserId ?? user.Id
                });
            }
            else
            {
                existing.IsGranted = request.IsGranted;
                existing.GrantedByUserId = currentUser.UserId ?? existing.GrantedByUserId;
                existing.GrantedAt = DateTimeOffset.UtcNow;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<PermissionGrantResponse>.Success(ToGrantResponse(
            "User",
            user.Id,
            user.UserPermissions.Where(x => x.IsGranted).Select(x => x.Permission.PermissionKey),
            user.UserPermissions.Where(x => !x.IsGranted).Select(x => x.Permission.PermissionKey)));
    }

    private async Task<Result> ValidatePermissionKeysAndCeilingAsync(
        IReadOnlyCollection<string> permissionKeys,
        CancellationToken cancellationToken)
    {
        var distinctKeys = permissionKeys
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var knownKeys = await dbContext.Permissions
            .Where(x => distinctKeys.Contains(x.PermissionKey))
            .Select(x => x.PermissionKey)
            .ToListAsync(cancellationToken);

        var missingKeys = distinctKeys.Except(knownKeys, StringComparer.OrdinalIgnoreCase).ToArray();
        if (missingKeys.Length > 0)
        {
            return Result.Failure(new Error("PERMISSION_KEYS_INVALID", $"Unknown permission keys: {string.Join(", ", missingKeys)}."));
        }

        if (currentUser.IsSuperAdmin)
        {
            return Result.Success();
        }

        var currentPermissions = currentUser.Permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var overCeiling = distinctKeys.Where(x => !currentPermissions.Contains(x)).ToArray();
        if (overCeiling.Length > 0)
        {
            return Result.Failure(new Error("PERMISSION_CEILING_EXCEEDED", $"Cannot grant permissions outside your ceiling: {string.Join(", ", overCeiling)}."));
        }

        return Result.Success();
    }

    private async Task<Result<Guid?>> ResolveTenantIdAsync(Guid? requestedTenantId, CancellationToken cancellationToken)
    {
        if (currentUser.IsSuperAdmin)
        {
            if (requestedTenantId.HasValue)
            {
                var tenantExists = await dbContext.Tenants.AnyAsync(x => x.Id == requestedTenantId.Value, cancellationToken);
                if (!tenantExists)
                {
                    return Result<Guid?>.Failure(new Error("TENANT_NOT_FOUND", "Tenant was not found."));
                }
            }

            return Result<Guid?>.Success(requestedTenantId);
        }

        if (!currentUser.TenantId.HasValue)
        {
            return Result<Guid?>.Failure(new Error("TENANT_REQUIRED", "Tenant context is required."));
        }

        if (requestedTenantId.HasValue && requestedTenantId.Value != currentUser.TenantId.Value)
        {
            return Result<Guid?>.Failure(new Error("TENANT_FORBIDDEN", "You cannot manage another tenant."));
        }

        return Result<Guid?>.Success(currentUser.TenantId.Value);
    }

    private bool CanAccessTenant(Guid? targetTenantId)
    {
        return currentUser.IsSuperAdmin || (currentUser.TenantId.HasValue && targetTenantId == currentUser.TenantId.Value);
    }

    private bool CanManageUserMenus(ApplicationUser targetUser)
    {
        if (!CanAccessTenant(targetUser.TenantId))
        {
            return false;
        }

        if (currentUser.IsSuperAdmin)
        {
            return true;
        }

        return targetUser.CreatedByUserId.HasValue && targetUser.CreatedByUserId == currentUser.UserId;
    }

    private async Task<Result<IReadOnlySet<string>>> GetAssignableMenuKeysForCurrentUserAsync(CancellationToken cancellationToken)
    {
        var items = await dbContext.NavigationItems
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);
        if (items.Count == 0)
        {
            return Result<IReadOnlySet<string>>.Failure(new Error("NAVIGATION_CONFIG_MISSING", "Navigation configuration is missing."));
        }

        var currentAssignment = await ReadUserAssignmentAsync(currentUser.UserId ?? Guid.Empty, cancellationToken);
        var allowedByAssignment = currentAssignment.MenuItemKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var permissionSet = currentUser.Permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var keys = items
            .Where(x =>
                currentUser.IsSuperAdmin
                || (ReadPermissions(x.RequiredPermissionsJson).Count == 0
                    || ReadPermissions(x.RequiredPermissionsJson).Any(permissionSet.Contains)))
            .Where(x => currentUser.IsSuperAdmin || allowedByAssignment.Count == 0 || allowedByAssignment.Contains(x.Key))
            .Select(x => x.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Result<IReadOnlySet<string>>.Success(keys);
    }

    private async Task<AssignmentReadModel> ReadUserAssignmentAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return new AssignmentReadModel([], DateTimeOffset.UtcNow);
        }

        var settingKey = $"{NavigationAssignmentSettingPrefix}{userId:N}";
        var setting = await dbContext.SystemSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.SettingKey == settingKey, cancellationToken);
        if (setting is null || string.IsNullOrWhiteSpace(setting.Value))
        {
            return new AssignmentReadModel([], DateTimeOffset.UtcNow);
        }

        try
        {
            var payload = JsonSerializer.Deserialize<UserNavigationAssignmentPayload>(setting.Value, JsonOptions);
            var keys = (payload?.MenuItemKeys ?? [])
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return new AssignmentReadModel(keys, setting.UpdatedAt ?? setting.CreatedAt);
        }
        catch (JsonException)
        {
            return new AssignmentReadModel([], setting.UpdatedAt ?? setting.CreatedAt);
        }
    }

    private async Task UpsertUserAssignmentAsync(Guid userId, IReadOnlyList<string> menuItemKeys, CancellationToken cancellationToken)
    {
        var settingKey = $"{NavigationAssignmentSettingPrefix}{userId:N}";
        var setting = await dbContext.SystemSettings
            .SingleOrDefaultAsync(x => x.SettingKey == settingKey, cancellationToken);
        var value = JsonSerializer.Serialize(new UserNavigationAssignmentPayload(menuItemKeys), JsonOptions);
        if (setting is null)
        {
            dbContext.SystemSettings.Add(new SystemSetting
            {
                SettingKey = settingKey,
                GroupName = "Platform",
                DisplayName = $"Menu assignment for user {userId}",
                Value = value,
                ValueType = "Json",
                Description = "User-level menu assignment for permission-based navigation.",
                IsEditable = true
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        setting.Value = value;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static RoleResponse Map(Role role)
    {
        return new RoleResponse(
            role.Id,
            role.TenantId,
            role.Name,
            role.IsSystemRole,
            role.RolePermissions.Select(x => x.Permission.PermissionKey).OrderBy(x => x).ToArray());
    }

    private static UserResponse Map(ApplicationUser user)
    {
        return new UserResponse(
            user.Id,
            user.TenantId,
            user.RoleId,
            user.Role.Name,
            user.FirstName,
            user.LastName,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.IsActive,
            user.IsEmailVerified,
            user.UserPermissions.Where(x => x.IsGranted).Select(x => x.Permission.PermissionKey).OrderBy(x => x).ToArray());
    }

    private static PermissionGrantResponse ToGrantResponse(
        string targetType,
        Guid targetId,
        IEnumerable<string> granted,
        IEnumerable<string> denied)
    {
        return new PermissionGrantResponse(
            targetType,
            targetId,
            granted.OrderBy(x => x).ToArray(),
            denied.OrderBy(x => x).ToArray());
    }

    private static List<ValidationError> ValidateCreateRole(CreateRoleRequest request)
    {
        var errors = new List<ValidationError>();
        Required(request.Name, nameof(request.Name), errors);
        if (request.PermissionKeys is null || request.PermissionKeys.Count == 0)
        {
            errors.Add(new ValidationError(nameof(request.PermissionKeys), "At least one permission is required."));
        }

        return errors;
    }

    private static List<ValidationError> ValidateCreateUser(CreateUserRequest request)
    {
        var errors = new List<ValidationError>();
        Required(request.FirstName, nameof(request.FirstName), errors);
        Required(request.LastName, nameof(request.LastName), errors);
        Required(request.Email, nameof(request.Email), errors);
        Required(request.Password, nameof(request.Password), errors);

        if (request.RoleId == Guid.Empty)
        {
            errors.Add(new ValidationError(nameof(request.RoleId), "RoleId is required."));
        }

        if (!string.IsNullOrEmpty(request.Password) && request.Password.Length < 8)
        {
            errors.Add(new ValidationError(nameof(request.Password), "Password must be at least 8 characters."));
        }

        return errors;
    }

    private static List<ValidationError> ValidateGrantPermissions(GrantPermissionsRequest request)
    {
        var errors = new List<ValidationError>();
        Required(request.TargetType, nameof(request.TargetType), errors);

        if (request.TargetId == Guid.Empty)
        {
            errors.Add(new ValidationError(nameof(request.TargetId), "TargetId is required."));
        }

        if (request.PermissionKeys is null || request.PermissionKeys.Count == 0)
        {
            errors.Add(new ValidationError(nameof(request.PermissionKeys), "At least one permission is required."));
        }

        return errors;
    }

    private static void Required(string? value, string field, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationError(field, $"{field} is required."));
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed record UserNavigationAssignmentPayload(IReadOnlyList<string> MenuItemKeys);

    private sealed record AssignmentReadModel(IReadOnlyList<string> MenuItemKeys, DateTimeOffset UpdatedAt);

    private static IReadOnlyList<string> ReadPermissions(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var values = JsonSerializer.Deserialize<string[]>(json, JsonOptions) ?? [];
            return values
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
