namespace HealthCareMS.Infrastructure.Notifications;

public sealed class SmtpOptions
{
    public const string SectionName = "Notifications:Smtp";

    public bool Enabled { get; init; }

    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 25;

    public string? Username { get; init; }

    public string? Password { get; init; }

    public string FromEmail { get; init; } = "no-reply@healthcarems.local";

    public string FromName { get; init; } = "HealthCareMS";

    public bool UseSsl { get; init; }
}

public sealed class TwilioOptions
{
    public const string SectionName = "Notifications:Twilio";

    public bool Enabled { get; init; }

    public string AccountSid { get; init; } = string.Empty;

    public string AuthToken { get; init; } = string.Empty;

    public string FromNumber { get; init; } = string.Empty;
}
