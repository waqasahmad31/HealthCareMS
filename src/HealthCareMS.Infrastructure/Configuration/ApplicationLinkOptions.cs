namespace HealthCareMS.Infrastructure.Configuration;

public sealed class ApplicationLinkOptions
{
    public const string SectionName = "ApplicationLinks";

    public string ApiBaseUrl { get; set; } = string.Empty;

    public string ClientBaseUrl { get; set; } = string.Empty;

    public string ConsultationWaitingRoomPathTemplate { get; set; } = "/consultation/waiting-room/{AppointmentId}";

    public string PrescriptionVerificationPathTemplate { get; set; } = "/api/v1/consultations/prescriptions/{PrescriptionId}/verify?code={VerificationCode}";
}
