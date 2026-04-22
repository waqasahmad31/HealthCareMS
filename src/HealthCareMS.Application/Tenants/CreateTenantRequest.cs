namespace HealthCareMS.Application.Tenants;

public sealed record CreateTenantRequest(
    string Name,
    string TenantType,
    string? LicenseNumber,
    string OwnerName,
    string? OwnerCnic,
    string Phone,
    string Email,
    string Address,
    string SubscriptionPlan,
    int MaxUsers,
    string EnabledModules,
    string PaymentGateways);
