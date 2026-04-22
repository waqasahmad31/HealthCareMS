using HealthCareMS.Domain.Common;
using HealthCareMS.Domain.Identity;

namespace HealthCareMS.Domain.Notifications;

public sealed class NotificationPreference : BaseEntity
{
    public Guid UserId { get; set; }

    public bool EmailEnabled { get; set; } = true;

    public bool SmsEnabled { get; set; } = true;

    public bool InAppEnabled { get; set; } = true;

    public bool Reminder24HourEnabled { get; set; } = true;

    public bool Reminder2HourEnabled { get; set; } = true;

    public ApplicationUser User { get; set; } = null!;
}
