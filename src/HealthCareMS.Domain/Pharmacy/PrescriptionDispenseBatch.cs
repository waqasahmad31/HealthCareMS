using HealthCareMS.Domain.Common;

namespace HealthCareMS.Domain.Pharmacy;

public sealed class PrescriptionDispenseBatch : BaseEntity
{
    public Guid PrescriptionDispenseItemId { get; set; }

    public Guid StockBatchId { get; set; }

    public string BatchNumber { get; set; } = string.Empty;

    public int QuantityDispensed { get; set; }

    public PrescriptionDispenseItem PrescriptionDispenseItem { get; set; } = null!;

    public StockBatch StockBatch { get; set; } = null!;
}
