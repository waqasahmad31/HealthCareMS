using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Common;
using HealthCareMS.Domain.Doctors;
using HealthCareMS.Domain.Patients;

namespace HealthCareMS.Domain.Consultations;

public enum ConsultationSessionStatus
{
    Waiting = 1,
    InProgress = 2,
    Completed = 3,
    Expired = 4
}

public sealed class ConsultationSession : BaseEntity
{
    public Guid AppointmentId { get; set; }

    public Guid PatientId { get; set; }

    public Guid DoctorId { get; set; }

    public string ChannelName { get; set; } = string.Empty;

    public string MeetingLink { get; set; } = string.Empty;

    public ConsultationSessionStatus Status { get; set; } = ConsultationSessionStatus.Waiting;

    public DateTimeOffset? PatientJoinedAt { get; set; }

    public DateTimeOffset? DoctorJoinedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }

    public DateTimeOffset LastTokenIssuedAt { get; set; }

    public Appointment Appointment { get; set; } = null!;

    public Patient Patient { get; set; } = null!;

    public Doctor Doctor { get; set; } = null!;
}
