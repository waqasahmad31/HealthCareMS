using HealthCareMS.Domain.Consultations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthCareMS.Infrastructure.Persistence.Configurations.Consultations;

public sealed class ConsultationSessionConfiguration : IEntityTypeConfiguration<ConsultationSession>
{
    public void Configure(EntityTypeBuilder<ConsultationSession> builder)
    {
        builder.ToTable("ConsultationSessions", DatabaseSchemas.Consultation);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ChannelName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.MeetingLink).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).HasDefaultValue(ConsultationSessionStatus.Waiting).IsRequired();

        builder.HasIndex(x => x.AppointmentId).IsUnique();
        builder.HasIndex(x => x.ChannelName).IsUnique();
        builder.HasIndex(x => new { x.DoctorId, x.Status });
        builder.HasIndex(x => new { x.PatientId, x.Status });
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.Appointment)
            .WithMany()
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

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
