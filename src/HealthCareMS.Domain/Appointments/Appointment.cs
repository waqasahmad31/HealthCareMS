using HealthCareMS.Domain.Common;
using HealthCareMS.Domain.Doctors;
using HealthCareMS.Domain.Patients;

namespace HealthCareMS.Domain.Appointments;

public enum AppointmentType
{
    OnSite = 1,
    Online = 2
}

public enum AppointmentStatus
{
    Pending = 1,
    Confirmed = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
    NoShow = 6
}

public enum AppointmentPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4
}

public enum PaymentStatus
{
    Pending = 1,
    Paid = 2,
    Refunded = 3
}

public sealed class Appointment : BaseEntity
{
    public string AppointmentNumber { get; set; } = string.Empty;

    public Guid PatientId { get; set; }

    public Guid DoctorId { get; set; }

    public DateTimeOffset ScheduledAt { get; set; }

    public DateTimeOffset EndAt { get; set; }

    public short DurationMinutes { get; set; } = 30;

    public AppointmentType Type { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

    public AppointmentPriority Priority { get; set; } = AppointmentPriority.Normal;

    public string? ReasonForVisit { get; set; }

    public string? PatientNotes { get; set; }

    public string? Diagnosis { get; set; }

    public string? Icd10Code { get; set; }

    public string? Icd10Title { get; set; }

    public string? ClinicalNotes { get; set; }

    public DateOnly? FollowUpDate { get; set; }

    public string? CancellationReason { get; set; }

    public string? CancelledBy { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public decimal ConsultationFee { get; set; }

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public string? MeetingLink { get; set; }

    public int? QueueNumber { get; set; }

    public DateTimeOffset? CheckedInAt { get; set; }

    public Patient Patient { get; set; } = null!;

    public Doctor Doctor { get; set; } = null!;
}
