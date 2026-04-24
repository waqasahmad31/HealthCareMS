using HealthCareMS.Domain.Common;
using HealthCareMS.Domain.Identity;

namespace HealthCareMS.Domain.Labs;

public enum LabResultStatus
{
    Draft = 1,
    Entered = 2,
    TechValidated = 3,
    ManagerValidated = 4,
    Released = 5,
    Corrected = 6
}

public sealed class LabTestResult : BaseEntity
{
    public Guid LabSampleBookingId { get; set; }

    public Guid LabBookingItemId { get; set; }

    public Guid LabTestId { get; set; }

    public string ResultNumber { get; set; } = string.Empty;

    public LabResultStatus Status { get; set; } = LabResultStatus.Draft;

    public string ParametersJson { get; set; } = "[]";

    public string? Summary { get; set; }

    public bool IsAbnormal { get; set; }

    public bool HasCriticalValue { get; set; }

    public string? CriticalValueSummary { get; set; }

    public DateTimeOffset? AutoValidatedAt { get; set; }

    public Guid? EnteredByUserId { get; set; }

    public DateTimeOffset? EnteredAt { get; set; }

    public Guid? TechnicianValidatedByUserId { get; set; }

    public DateTimeOffset? TechnicianValidatedAt { get; set; }

    public Guid? ManagerValidatedByUserId { get; set; }

    public DateTimeOffset? ManagerValidatedAt { get; set; }

    public Guid? ReleasedByUserId { get; set; }

    public DateTimeOffset? ReleasedAt { get; set; }

    public DateTimeOffset? CriticalAlertSentAt { get; set; }

    public Guid? CriticalAlertAcknowledgedByUserId { get; set; }

    public DateTimeOffset? CriticalAlertAcknowledgedAt { get; set; }

    public string? AddendumNotes { get; set; }

    public Guid? AddendumByUserId { get; set; }

    public DateTimeOffset? AddendumAt { get; set; }

    public LabSampleBooking LabSampleBooking { get; set; } = null!;

    public LabBookingItem LabBookingItem { get; set; } = null!;

    public LabTest LabTest { get; set; } = null!;

    public ApplicationUser? EnteredByUser { get; set; }

    public ApplicationUser? TechnicianValidatedByUser { get; set; }

    public ApplicationUser? ManagerValidatedByUser { get; set; }

    public ApplicationUser? ReleasedByUser { get; set; }

    public ApplicationUser? CriticalAlertAcknowledgedByUser { get; set; }

    public ApplicationUser? AddendumByUser { get; set; }
}
