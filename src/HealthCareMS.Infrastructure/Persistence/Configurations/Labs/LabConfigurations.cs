using HealthCareMS.Domain.Labs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthCareMS.Infrastructure.Persistence.Configurations.Labs;

public sealed class LabTestConfiguration : IEntityTypeConfiguration<LabTest>
{
    public void Configure(EntityTypeBuilder<LabTest> builder)
    {
        builder.ToTable("LabTests", DatabaseSchemas.Lab);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TestCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.TestName).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(120).IsRequired();
        builder.Property(x => x.SampleType).HasMaxLength(80).IsRequired();
        builder.Property(x => x.PreparationInstructions).HasMaxLength(1000);
        builder.Property(x => x.Price).HasPrecision(12, 2);
        builder.Property(x => x.HomeCollectionExtra).HasPrecision(12, 2);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.TestCode).IsUnique();
        builder.HasIndex(x => new { x.TestName, x.Category });
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class LabSampleBookingConfiguration : IEntityTypeConfiguration<LabSampleBooking>
{
    public void Configure(EntityTypeBuilder<LabSampleBooking> builder)
    {
        builder.ToTable("SampleBookings", DatabaseSchemas.Lab);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BookingNumber).HasMaxLength(35).IsRequired();
        builder.Property(x => x.CollectionType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.CollectionAddress).HasMaxLength(4000);
        builder.Property(x => x.SampleBarcode).HasMaxLength(120);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.SubTotal).HasPrecision(12, 2);
        builder.Property(x => x.HomeCollectionFee).HasPrecision(12, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(12, 2);

        builder.HasIndex(x => x.BookingNumber).IsUnique();
        builder.HasIndex(x => x.AppointmentId);
        builder.HasIndex(x => x.PatientId);
        builder.HasIndex(x => x.SampleBarcode).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Appointment)
            .WithMany()
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Prescription)
            .WithMany()
            .HasForeignKey(x => x.PrescriptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class LabBookingItemConfiguration : IEntityTypeConfiguration<LabBookingItem>
{
    public void Configure(EntityTypeBuilder<LabBookingItem> builder)
    {
        builder.ToTable("BookingItems", DatabaseSchemas.Lab);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Price).HasPrecision(12, 2);
        builder.HasIndex(x => new { x.BookingId, x.LabTestId }).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.Booking)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.LabTest)
            .WithMany()
            .HasForeignKey(x => x.LabTestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
