using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Common;
using HealthCareMS.Domain.Patients;

namespace HealthCareMS.Domain.Doctors;

public sealed class DoctorReview : BaseEntity
{
    public Guid AppointmentId { get; set; }

    public Guid PatientId { get; set; }

    public Guid DoctorId { get; set; }

    public byte Rating { get; set; }

    public string? ReviewText { get; set; }

    public bool IsRecommended { get; set; }

    public DateTimeOffset ReviewedAt { get; set; } = DateTimeOffset.UtcNow;

    public Appointment Appointment { get; set; } = null!;

    public Patient Patient { get; set; } = null!;

    public Doctor Doctor { get; set; } = null!;
}
