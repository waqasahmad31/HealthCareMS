using HealthCareMS.Domain.Common;

namespace HealthCareMS.Domain.Labs;

public sealed class LabPanelItem : BaseEntity
{
    public Guid LabPanelId { get; set; }

    public Guid LabTestId { get; set; }

    public LabPanel LabPanel { get; set; } = null!;

    public LabTest LabTest { get; set; } = null!;
}
