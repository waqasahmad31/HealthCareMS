using HealthCareMS.Domain.Appointments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthCareMS.Infrastructure.Persistence.Configurations.Appointments;

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments", DatabaseSchemas.Appointment);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AppointmentNumber).HasMaxLength(30).IsRequired();
        builder.Property(x => x.DurationMinutes).HasDefaultValue((short)30);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).HasDefaultValue(AppointmentStatus.Pending).IsRequired();
        builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20).HasDefaultValue(AppointmentPriority.Normal).IsRequired();
        builder.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(20).HasDefaultValue(PaymentStatus.Pending).IsRequired();
        builder.Property(x => x.ReasonForVisit).HasMaxLength(1000);
        builder.Property(x => x.PatientNotes).HasMaxLength(2000);
        builder.Property(x => x.CancellationReason).HasMaxLength(1000);
        builder.Property(x => x.CancelledBy).HasMaxLength(20);
        builder.Property(x => x.ConsultationFee).HasPrecision(10, 2);

        builder.HasIndex(x => x.AppointmentNumber).IsUnique();
        builder.HasIndex(x => new { x.DoctorId, x.ScheduledAt });
        builder.HasIndex(x => new { x.PatientId, x.Status });
        builder.HasIndex(x => new { x.DoctorId, x.ScheduledAt, x.EndAt });
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Doctor)
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
