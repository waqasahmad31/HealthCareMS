namespace HealthCareMS.Infrastructure.Configuration;

public interface IApplicationLinkBuilder
{
    string BuildConsultationWaitingRoomUrl(Guid appointmentId);

    string BuildPrescriptionVerificationUrl(Guid prescriptionId, string verificationCode);
}
