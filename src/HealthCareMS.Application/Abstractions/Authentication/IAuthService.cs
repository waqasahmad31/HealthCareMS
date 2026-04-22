using HealthCareMS.Application.Auth;
using HealthCareMS.Shared.Common;

namespace HealthCareMS.Application.Abstractions.Authentication;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
