using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Common;
using HealthCareMS.Domain.Doctors;
using HealthCareMS.Domain.Patients;

namespace HealthCareMS.Domain.Consultations;

public enum PrescriptionStatus
{
    Issued = 1,
    Cancelled = 2,
    Expired = 3
}

public sealed class Prescription : BaseEntity
{
    public string PrescriptionNumber { get; set; } = string.Empty;

    public Guid AppointmentId { get; set; }

    public Guid PatientId { get; set; }

    public Guid DoctorId { get; set; }

    public string Diagnosis { get; set; } = string.Empty;

    public string? Icd10Code { get; set; }

    public string? Icd10Title { get; set; }

    public string? ClinicalNotes { get; set; }

    public DateOnly? FollowUpDate { get; set; }

    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ValidUntil { get; set; } = DateTimeOffset.UtcNow.AddDays(30);

    public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Issued;

    public string VerificationCode { get; set; } = string.Empty;

    public string DigitalSignature { get; set; } = string.Empty;

    public Appointment Appointment { get; set; } = null!;

    public Patient Patient { get; set; } = null!;

    public Doctor Doctor { get; set; } = null!;

    public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
}
