using HealthCareMS.Domain.Doctors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthCareMS.Infrastructure.Persistence.Configurations.Doctors;

public sealed class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctors", DatabaseSchemas.Doctor);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PmdcRegistrationNumber).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Specialization).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Qualification).HasMaxLength(300);
        builder.Property(x => x.City).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ConsultationFee).HasPrecision(10, 2);

        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasIndex(x => x.PmdcRegistrationNumber).IsUnique();
        builder.HasIndex(x => new { x.Specialization, x.City });
        builder.HasIndex(x => new { x.IsVerified, x.IsActive });
        builder.HasIndex(x => new { x.City, x.IsActive });
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<Doctor>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorSchedule> builder)
    {
        builder.ToTable("DoctorSchedules", DatabaseSchemas.Doctor);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DayOfWeek).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(x => new { x.DoctorId, x.DayOfWeek });
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.Doctor)
            .WithMany(x => x.Schedules)
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
