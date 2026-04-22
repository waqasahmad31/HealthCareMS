namespace HealthCareMS.Application.Tenants;

public sealed record TenantResponse(
    Guid Id,
    string Name,
    string TenantType,
    string? LicenseNumber,
    string OwnerName,
    string Phone,
    string Email,
    bool IsActive,
    string SubscriptionPlan,
    int MaxUsers,
    DateTimeOffset CreatedAt);
