namespace HealthCareMS.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "HealthCareMS";

    public string Audience { get; init; } = "HealthCareMS.Client";

    public string SigningKey { get; init; } = "CHANGE_ME_TO_A_32_CHARACTER_MINIMUM_SECRET";

    public int AccessTokenMinutes { get; init; } = 60;
}
