namespace HealthCareMS.Application.Abstractions.Tenancy;

public interface ICurrentUser
{
    Guid? UserId { get; }

    Guid? TenantId { get; }

    bool IsAuthenticated { get; }

    bool IsSuperAdmin { get; }

    IReadOnlyCollection<string> Permissions { get; }
}
