namespace HealthCareMS.Domain.Common;

public interface ITenantScoped
{
    Guid TenantId { get; }
}
