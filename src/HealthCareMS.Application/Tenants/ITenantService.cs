using HealthCareMS.Shared.Common;

namespace HealthCareMS.Application.Tenants;

public interface ITenantService
{
    Task<Result<TenantResponse>> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<TenantResponse>> GetAllAsync(CancellationToken cancellationToken);
}
