namespace HealthCareMS.Application.Consultations;

public interface IConsultationSummaryDocumentService
{
    byte[] GenerateSummaryPdf(ConsultationSummaryResponse summary);
}
