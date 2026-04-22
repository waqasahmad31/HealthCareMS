using HealthCareMS.Domain.Common;

namespace HealthCareMS.Domain.Doctors;

public sealed class DoctorSchedule : BaseEntity
{
    public Guid DoctorId { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public short SlotDurationMinutes { get; set; } = 30;

    public bool IsOnlineAvailable { get; set; } = true;

    public bool IsOnSiteAvailable { get; set; } = true;

    public Doctor Doctor { get; set; } = null!;
}
