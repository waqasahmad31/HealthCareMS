using HealthCareMS.Shared.Common;

namespace HealthCareMS.Application.Identity;

public interface IIdentityManagementService
{
    Task<IReadOnlyList<PermissionResponse>> GetPermissionsAsync(CancellationToken cancellationToken);

    Task<Result<RoleResponse>> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken);

    Task<Result<UserResponse>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken);

    Task<Result<PermissionGrantResponse>> GrantPermissionsAsync(GrantPermissionsRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<ManageableUserResponse>> GetManageableUsersAsync(string? search, CancellationToken cancellationToken);

    Task<Result<UserMenuAssignmentResponse>> GetUserMenuAssignmentAsync(Guid userId, CancellationToken cancellationToken);

    Task<Result<UserMenuAssignmentResponse>> AssignUserMenuAsync(
        Guid userId,
        AssignUserMenuRequest request,
        CancellationToken cancellationToken);
}
