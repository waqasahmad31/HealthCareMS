using HealthCareMS.Domain.Consultations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthCareMS.Infrastructure.Persistence.Configurations.Consultations;

public sealed class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("Prescriptions", DatabaseSchemas.Consultation);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PrescriptionNumber).HasMaxLength(35).IsRequired();
        builder.Property(x => x.Diagnosis).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Icd10Code).HasMaxLength(20);
        builder.Property(x => x.Icd10Title).HasMaxLength(300);
        builder.Property(x => x.ClinicalNotes).HasMaxLength(4000);
        builder.Property(x => x.VerificationCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.DigitalSignature).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValue(PrescriptionStatus.Issued).IsRequired();

        builder.HasIndex(x => x.PrescriptionNumber).IsUnique();
        builder.HasIndex(x => x.VerificationCode).IsUnique();
        builder.HasIndex(x => x.AppointmentId).IsUnique();
        builder.HasIndex(x => new { x.PatientId, x.IssuedAt });
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
