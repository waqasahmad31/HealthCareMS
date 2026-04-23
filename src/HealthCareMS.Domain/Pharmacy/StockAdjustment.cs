using HealthCareMS.Domain.Common;
using HealthCareMS.Domain.Identity;

namespace HealthCareMS.Domain.Pharmacy;

public enum StockAdjustmentType
{
    Increase = 1,
    Decrease = 2,
    Correction = 3,
    Damaged = 4,
    Expired = 5,
    Dispense = 6
}

public sealed class StockAdjustment : BaseEntity
{
    public Guid? TenantId { get; set; }

    public Guid MedicineId { get; set; }

    public Guid StockBatchId { get; set; }

    public StockAdjustmentType AdjustmentType { get; set; } = StockAdjustmentType.Correction;

    public int QuantityDelta { get; set; }

    public int PreviousQuantity { get; set; }

    public int NewQuantity { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTimeOffset AdjustedAt { get; set; } = DateTimeOffset.UtcNow;

    public Tenant? Tenant { get; set; }

    public Medicine Medicine { get; set; } = null!;

    public StockBatch StockBatch { get; set; } = null!;
}
