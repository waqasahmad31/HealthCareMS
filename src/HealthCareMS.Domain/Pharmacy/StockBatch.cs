using HealthCareMS.Domain.Common;
using HealthCareMS.Domain.Identity;

namespace HealthCareMS.Domain.Pharmacy;

public sealed class StockBatch : BaseEntity
{
    public Guid? TenantId { get; set; }

    public Guid MedicineId { get; set; }

    public Guid? SupplierId { get; set; }

    public string BatchNumber { get; set; } = string.Empty;

    public DateOnly? ManufacturedDate { get; set; }

    public DateOnly ExpiryDate { get; set; }

    public int QuantityOnHand { get; set; }

    public decimal UnitCostPrice { get; set; }

    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;

    public Tenant? Tenant { get; set; }

    public Medicine Medicine { get; set; } = null!;

    public Supplier? Supplier { get; set; }
}
