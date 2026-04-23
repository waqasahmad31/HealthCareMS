using HealthCareMS.Application.Abstractions.Persistence;
using HealthCareMS.Application.Abstractions.Tenancy;
using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Common;
using HealthCareMS.Domain.Consultations;
using HealthCareMS.Domain.Doctors;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Domain.Notifications;
using HealthCareMS.Domain.Patients;
using Microsoft.EntityFrameworkCore;

namespace HealthCareMS.Infrastructure.Persistence;

public sealed class HealthCareDbContext : DbContext, IUnitOfWork
{
    private readonly ICurrentUser? currentUser;

    public HealthCareDbContext(DbContextOptions<HealthCareDbContext> options, ICurrentUser? currentUser = null)
        : base(options)
    {
        this.currentUser = currentUser;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();

    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<MedicalHistory> MedicalHistories => Set<MedicalHistory>();

    public DbSet<PatientVital> PatientVitals => Set<PatientVital>();

    public DbSet<Doctor> Doctors => Set<Doctor>();

    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<Prescription> Prescriptions => Set<Prescription>();

    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();

    public DbSet<DrapMedicine> DrapMedicines => Set<DrapMedicine>();

    public DbSet<ConsultationSession> ConsultationSessions => Set<ConsultationSession>();

    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HealthCareDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditValues();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditValues()
    {
        var now = DateTimeOffset.UtcNow;
        var userId = currentUser?.UserId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedByUserId ??= userId;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedByUserId = userId;
            }

            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = now;
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedByUserId = userId;
            }
        }
    }
}
