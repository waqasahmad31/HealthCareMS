using HealthCareMS.Domain.Common;

namespace HealthCareMS.Domain.Identity;

public sealed class UserNavigationAssignment : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid NavigationItemId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public NavigationItem NavigationItem { get; set; } = null!;
}
