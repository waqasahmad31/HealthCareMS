namespace HealthCareMS.Infrastructure.Consultations;

public sealed class ConsultationSessionOptions
{
    public const string SectionName = "Agora";

    public string AppId { get; set; } = "local-dev-app-id";

    public string AppCertificate { get; set; } = "local-dev-app-certificate";

    public int TokenExpiryMinutes { get; set; } = 60;

    public string ClientBaseUrl { get; set; } = "http://localhost:5157";
}
