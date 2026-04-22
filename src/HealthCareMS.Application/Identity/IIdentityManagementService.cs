using HealthCareMS.Shared.Common;

namespace HealthCareMS.Application.Identity;

public interface IIdentityManagementService
{
    Task<IReadOnlyList<PermissionResponse>> GetPermissionsAsync(CancellationToken cancellationToken);

    Task<Result<RoleResponse>> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken);

    Task<Result<UserResponse>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken);

    Task<Result<PermissionGrantResponse>> GrantPermissionsAsync(GrantPermissionsRequest request, CancellationToken cancellationToken);
}
