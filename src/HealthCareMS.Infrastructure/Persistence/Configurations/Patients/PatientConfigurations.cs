using HealthCareMS.Domain.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthCareMS.Infrastructure.Persistence.Configurations.Patients;

public sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients", DatabaseSchemas.Patient);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Cnic).HasMaxLength(15);
        builder.Property(x => x.Gender).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.BloodGroup).HasMaxLength(10);
        builder.Property(x => x.Phone).HasMaxLength(20);
        builder.Property(x => x.AlternatePhone).HasMaxLength(20);
        builder.Property(x => x.AddressStreet).HasMaxLength(200);
        builder.Property(x => x.AddressCity).HasMaxLength(100);
        builder.Property(x => x.AddressProvince).HasMaxLength(100);
        builder.Property(x => x.AddressPostalCode).HasMaxLength(20);
        builder.Property(x => x.EmergencyContactName).HasMaxLength(200);
        builder.Property(x => x.EmergencyContactPhone).HasMaxLength(20);
        builder.Property(x => x.EmergencyContactRelation).HasMaxLength(50);
        builder.Property(x => x.InsuranceProvider).HasMaxLength(200);
        builder.Property(x => x.InsurancePolicyNo).HasMaxLength(100);

        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasIndex(x => x.Cnic).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<Patient>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MedicalHistoryConfiguration : IEntityTypeConfiguration<MedicalHistory>
{
    public void Configure(EntityTypeBuilder<MedicalHistory> builder)
    {
        builder.ToTable("MedicalHistories", DatabaseSchemas.Patient);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Allergies).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb").IsRequired();
        builder.Property(x => x.ChronicDiseases).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb").IsRequired();
        builder.Property(x => x.CurrentMedications).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb").IsRequired();
        builder.Property(x => x.PastSurgeries).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb").IsRequired();
        builder.Property(x => x.FamilyHistory).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb").IsRequired();
        builder.Property(x => x.SmokingStatus).HasMaxLength(30).HasDefaultValue("Non-Smoker");
        builder.Property(x => x.AlcoholStatus).HasMaxLength(30).HasDefaultValue("Non-User");

        builder.HasIndex(x => x.PatientId).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.Patient)
            .WithOne(x => x.MedicalHistory)
            .HasForeignKey<MedicalHistory>(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PatientVitalConfiguration : IEntityTypeConfiguration<PatientVital>
{
    public void Configure(EntityTypeBuilder<PatientVital> builder)
    {
        builder.ToTable("PatientVitals", DatabaseSchemas.Patient);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BloodSugarMgDl).HasPrecision(8, 2);
        builder.Property(x => x.BloodSugarContext).HasMaxLength(40);
        builder.Property(x => x.WeightKg).HasPrecision(8, 2);
        builder.Property(x => x.TemperatureCelsius).HasPrecision(5, 2);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasIndex(x => new { x.PatientId, x.RecordedAt });
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.Patient)
            .WithMany(x => x.Vitals)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
