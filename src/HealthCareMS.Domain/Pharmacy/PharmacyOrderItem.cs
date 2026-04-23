using HealthCareMS.Domain.Common;

namespace HealthCareMS.Domain.Pharmacy;

public sealed class PharmacyOrderItem : BaseEntity
{
    public Guid PharmacyOrderId { get; set; }

    public Guid MedicineId { get; set; }

    public string MedicineName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }

    public PharmacyOrder PharmacyOrder { get; set; } = null!;

    public Medicine Medicine { get; set; } = null!;
}
