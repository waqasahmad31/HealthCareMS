using HealthCareMS.Domain.Common;
using HealthCareMS.Domain.Consultations;
using HealthCareMS.Domain.Doctors;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Domain.Patients;

namespace HealthCareMS.Domain.Pharmacy;

public enum PrescriptionDispenseStatus
{
    Completed = 1,
    Voided = 2
}

public sealed class PrescriptionDispense : BaseEntity
{
    public Guid? TenantId { get; set; }

    public string DispenseNumber { get; set; } = string.Empty;

    public string ReceiptNumber { get; set; } = string.Empty;

    public Guid PrescriptionId { get; set; }

    public Guid PatientId { get; set; }

    public Guid DoctorId { get; set; }

    public string VerificationCode { get; set; } = string.Empty;

    public PrescriptionDispenseStatus Status { get; set; } = PrescriptionDispenseStatus.Completed;

    public DateTimeOffset DispensedAt { get; set; } = DateTimeOffset.UtcNow;

    public decimal SubTotal { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    public Tenant? Tenant { get; set; }

    public Prescription Prescription { get; set; } = null!;

    public Patient Patient { get; set; } = null!;

    public Doctor Doctor { get; set; } = null!;

    public ICollection<PrescriptionDispenseItem> Items { get; set; } = new List<PrescriptionDispenseItem>();
}
