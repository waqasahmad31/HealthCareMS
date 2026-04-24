using HealthCareMS.Shared.Common;

namespace HealthCareMS.Application.Security;

public interface ISecurityCenterService
{
    Task<Result<SecurityOverviewResponse>> GetOverviewAsync(Guid userId, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<UserAuthSessionResponse>>> GetActiveSessionsAsync(Guid userId, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<LoginActivityResponse>>> GetLoginHistoryAsync(Guid userId, CancellationToken cancellationToken);

    Task<Result<UserAuthSessionResponse>> RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);

    Task<Result<TwoFactorSetupResponse>> BeginTwoFactorSetupAsync(Guid userId, CancellationToken cancellationToken);

    Task<Result<TwoFactorSetupResponse>> EnableTwoFactorAsync(Guid userId, EnableTwoFactorRequest request, CancellationToken cancellationToken);

    Task<Result<SecurityOverviewResponse>> DisableTwoFactorAsync(Guid userId, CancellationToken cancellationToken);
}
