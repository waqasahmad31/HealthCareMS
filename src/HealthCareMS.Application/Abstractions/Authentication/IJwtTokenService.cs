using HealthCareMS.Domain.Identity;

namespace HealthCareMS.Application.Abstractions.Authentication;

public interface IJwtTokenService
{
    TokenResult CreateAccessToken(ApplicationUser user, IReadOnlyCollection<string> permissions);
}

public sealed record TokenResult(string AccessToken, DateTimeOffset ExpiresAt);
