using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Common;
using HealthCareMS.Domain.Consultations;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Domain.Patients;

namespace HealthCareMS.Domain.Labs;

public enum LabCollectionType
{
    OnSite = 1,
    Home = 2
}

public enum LabBookingStatus
{
    Ordered = 1,
    SampleCollected = 2,
    Cancelled = 3,
    CheckedIn = 4
}

public sealed class LabSampleBooking : BaseEntity
{
    public string BookingNumber { get; set; } = string.Empty;

    public Guid? TenantId { get; set; }

    public Guid PatientId { get; set; }

    public Guid? AppointmentId { get; set; }

    public Guid? PrescriptionId { get; set; }

    public LabCollectionType CollectionType { get; set; } = LabCollectionType.OnSite;

    public LabBookingStatus Status { get; set; } = LabBookingStatus.Ordered;

    public DateTimeOffset? CollectionScheduledAt { get; set; }

    public string? CollectionAddress { get; set; }

    public string? SampleBarcode { get; set; }

    public string? TokenNumber { get; set; }

    public bool? FastingVerified { get; set; }

    public DateTimeOffset? CheckedInAt { get; set; }

    public DateTimeOffset? BarcodeLabelGeneratedAt { get; set; }

    public string? Notes { get; set; }

    public decimal SubTotal { get; set; }

    public decimal HomeCollectionFee { get; set; }

    public decimal TotalAmount { get; set; }

    public Tenant? Tenant { get; set; }

    public Patient Patient { get; set; } = null!;

    public Appointment? Appointment { get; set; }

    public Prescription? Prescription { get; set; }

    public ICollection<LabBookingItem> Items { get; set; } = new List<LabBookingItem>();
}
