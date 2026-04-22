using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HealthCareMS.Application.Abstractions.Authentication;
using HealthCareMS.Domain.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HealthCareMS.Infrastructure.Authentication;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions jwtOptions = options.Value;

    public TokenResult CreateAccessToken(ApplicationUser user, IReadOnlyCollection<string> permissions)
    {
        if (jwtOptions.SigningKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters.");
        }

        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(jwtOptions.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.Name)
        };

        if (user.TenantId.HasValue)
        {
            claims.Add(new("TenantId", user.TenantId.Value.ToString()));
        }

        claims.AddRange(permissions.Select(permission => new Claim("Permission", permission)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            jwtOptions.Issuer,
            jwtOptions.Audience,
            claims,
            now.UtcDateTime,
            expiresAt.UtcDateTime,
            credentials);

        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
