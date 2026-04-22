namespace HealthCareMS.Infrastructure.Consultations;

public sealed class PrescriptionDocumentOptions
{
    public const string SectionName = "PrescriptionDocuments";

    public string VerificationBaseUrl { get; set; } = "http://localhost:5270/api/v1/consultations/prescriptions";
}
