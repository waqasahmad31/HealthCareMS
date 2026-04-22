using HealthCareMS.Domain.Common;
using HealthCareMS.Domain.Identity;

namespace HealthCareMS.Domain.Doctors;

public sealed class Doctor : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid? TenantId { get; set; }

    public string PmdcRegistrationNumber { get; set; } = string.Empty;

    public string Specialization { get; set; } = string.Empty;

    public string? Qualification { get; set; }

    public string? Biography { get; set; }

    public string City { get; set; } = string.Empty;

    public decimal ConsultationFee { get; set; }

    public bool IsVerified { get; set; }

    public bool IsActive { get; set; } = true;

    public ApplicationUser User { get; set; } = null!;

    public Tenant? Tenant { get; set; }

    public ICollection<DoctorSchedule> Schedules { get; set; } = new List<DoctorSchedule>();
}
