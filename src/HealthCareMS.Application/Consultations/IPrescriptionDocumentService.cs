using HealthCareMS.Domain.Consultations;

namespace HealthCareMS.Application.Consultations;

public interface IPrescriptionDocumentService
{
    byte[] GeneratePrescriptionPdf(Prescription prescription, string verificationUrl);
}
