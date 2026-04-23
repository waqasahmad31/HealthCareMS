using HealthCareMS.Domain.Common;
using HealthCareMS.Domain.Identity;

namespace HealthCareMS.Domain.Pharmacy;

public enum StockAlertType
{
    LowStock = 1,
    Expiry30Days = 2,
    Expiry60Days = 3,
    Expiry90Days = 4
}

public enum StockAlertStatus
{
    Open = 1,
    Resolved = 2
}

public sealed class StockAlert : BaseEntity
{
    public Guid? TenantId { get; set; }

    public Guid MedicineId { get; set; }

    public Guid? StockBatchId { get; set; }

    public StockAlertType AlertType { get; set; }

    public StockAlertStatus Status { get; set; } = StockAlertStatus.Open;

    public string Severity { get; set; } = "Info";

    public string Message { get; set; } = string.Empty;

    public int? ThresholdQuantity { get; set; }

    public int? QuantityOnHand { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public DateTimeOffset DetectedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ResolvedAt { get; set; }

    public Tenant? Tenant { get; set; }

    public Medicine Medicine { get; set; } = null!;

    public StockBatch? StockBatch { get; set; }
}
