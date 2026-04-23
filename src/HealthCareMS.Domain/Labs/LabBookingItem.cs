using HealthCareMS.Domain.Common;

namespace HealthCareMS.Domain.Labs;

public sealed class LabBookingItem : BaseEntity
{
    public Guid BookingId { get; set; }

    public Guid LabTestId { get; set; }

    public decimal Price { get; set; }

    public LabSampleBooking Booking { get; set; } = null!;

    public LabTest LabTest { get; set; } = null!;
}
