namespace HealthCareMS.Infrastructure.Consultations;

public sealed class ConsultationSessionOptions
{
    public const string SectionName = "Agora";

    public string AppId { get; set; } = string.Empty;

    public string AppCertificate { get; set; } = string.Empty;

    public int TokenExpiryMinutes { get; set; } = 60;

    public string ClientBaseUrl { get; set; } = string.Empty;
}
