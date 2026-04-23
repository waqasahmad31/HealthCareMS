using HealthCareMS.Domain.Common;
using HealthCareMS.Domain.Consultations;

namespace HealthCareMS.Domain.Pharmacy;

public sealed class PrescriptionDispenseItem : BaseEntity
{
    public Guid PrescriptionDispenseId { get; set; }

    public Guid PrescriptionItemId { get; set; }

    public Guid MedicineId { get; set; }

    public string PrescribedMedicineName { get; set; } = string.Empty;

    public string DispensedMedicineName { get; set; } = string.Empty;

    public decimal QuantityPrescribed { get; set; }

    public int QuantityDispensed { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }

    public PrescriptionDispense PrescriptionDispense { get; set; } = null!;

    public PrescriptionItem PrescriptionItem { get; set; } = null!;

    public Medicine Medicine { get; set; } = null!;

    public ICollection<PrescriptionDispenseBatch> Batches { get; set; } = new List<PrescriptionDispenseBatch>();
}
