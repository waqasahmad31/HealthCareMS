using HealthCareMS.Domain.Common;
using HealthCareMS.Domain.Consultations;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Domain.Patients;

namespace HealthCareMS.Domain.Pharmacy;

public enum PharmacyOrderStatus
{
    Placed = 1,
    Confirmed = 2,
    Prepared = 3,
    AssignedForDelivery = 4,
    Dispatched = 5,
    Delivered = 6,
    Cancelled = 7,
    Rejected = 8
}

public sealed class PharmacyOrder : BaseEntity
{
    public Guid? TenantId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public Guid PatientId { get; set; }

    public Guid? PrescriptionId { get; set; }

    public PharmacyOrderStatus Status { get; set; } = PharmacyOrderStatus.Placed;

    public DateTimeOffset OrderedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ReviewedAt { get; set; }

    public DateTimeOffset? ConfirmedAt { get; set; }

    public Guid? DeliveryAgentUserId { get; set; }

    public DateTimeOffset? AssignedAt { get; set; }

    public DateTimeOffset? DispatchedAt { get; set; }

    public DateTimeOffset? DeliveredAt { get; set; }

    public string DeliveryAddress { get; set; } = string.Empty;

    public DateTimeOffset? DeliveryWindowStart { get; set; }

    public DateTimeOffset? DeliveryWindowEnd { get; set; }

    public string? PrescriptionUploadFileName { get; set; }

    public string? PrescriptionUploadContentType { get; set; }

    public byte[]? PrescriptionUploadContent { get; set; }

    public string? PatientNotes { get; set; }

    public string? PharmacistNotes { get; set; }

    public decimal SubTotal { get; set; }

    public decimal DeliveryFee { get; set; }

    public decimal TotalAmount { get; set; }

    public Tenant? Tenant { get; set; }

    public Patient Patient { get; set; } = null!;

    public Prescription? Prescription { get; set; }

    public ApplicationUser? DeliveryAgentUser { get; set; }

    public ICollection<PharmacyOrderItem> Items { get; set; } = new List<PharmacyOrderItem>();
}
