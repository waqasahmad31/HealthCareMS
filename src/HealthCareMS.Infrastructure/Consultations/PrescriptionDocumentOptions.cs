namespace HealthCareMS.Infrastructure.Consultations;

public sealed class PrescriptionDocumentOptions
{
    public const string SectionName = "PrescriptionDocuments";

    public string VerificationBaseUrl { get; set; } = string.Empty;
}
