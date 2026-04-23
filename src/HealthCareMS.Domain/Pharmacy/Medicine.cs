using HealthCareMS.Domain.Common;
using HealthCareMS.Domain.Identity;

namespace HealthCareMS.Domain.Pharmacy;

public sealed class Medicine : BaseEntity
{
    public Guid? TenantId { get; set; }

    public string GenericName { get; set; } = string.Empty;

    public string BrandName { get; set; } = string.Empty;

    public string DosageForm { get; set; } = string.Empty;

    public string? Strength { get; set; }

    public string DrapRegistrationNumber { get; set; } = string.Empty;

    public string? Manufacturer { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal UnitCostPrice { get; set; }

    public bool IsControlled { get; set; }

    public int ReorderLevel { get; set; } = 10;

    public string Barcode { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public Tenant? Tenant { get; set; }

    public ICollection<StockBatch> StockBatches { get; set; } = new List<StockBatch>();
}
