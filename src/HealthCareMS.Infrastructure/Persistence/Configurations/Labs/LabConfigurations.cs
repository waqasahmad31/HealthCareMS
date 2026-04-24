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
        builder.Property(x => x.TokenNumber).HasMaxLength(35);
        builder.Property(x => x.ReportVerificationCode).HasMaxLength(80);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.CollectionStatusNotes).HasMaxLength(1000);
        builder.Property(x => x.SubTotal).HasPrecision(12, 2);
        builder.Property(x => x.HomeCollectionFee).HasPrecision(12, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(12, 2);

        builder.HasIndex(x => x.BookingNumber).IsUnique();
        builder.HasIndex(x => x.AppointmentId);
        builder.HasIndex(x => x.PatientId);
        builder.HasIndex(x => x.SampleBarcode).IsUnique();
        builder.HasIndex(x => x.TokenNumber);
        builder.HasIndex(x => x.CollectionAgentUserId);
        builder.HasIndex(x => x.ReportVerificationCode);
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

        builder
            .HasOne(x => x.CollectionAgentUser)
            .WithMany()
            .HasForeignKey(x => x.CollectionAgentUserId)
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

public sealed class LabPanelConfiguration : IEntityTypeConfiguration<LabPanel>
{
    public void Configure(EntityTypeBuilder<LabPanel> builder)
    {
        builder.ToTable("LabPanels", DatabaseSchemas.Lab);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PanelCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.PanelName).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Price).HasPrecision(12, 2);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.PanelCode).IsUnique();
        builder.HasIndex(x => new { x.PanelName, x.Category });
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class LabPanelItemConfiguration : IEntityTypeConfiguration<LabPanelItem>
{
    public void Configure(EntityTypeBuilder<LabPanelItem> builder)
    {
        builder.ToTable("LabPanelItems", DatabaseSchemas.Lab);
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.LabPanelId, x.LabTestId }).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.LabPanel)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.LabPanelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.LabTest)
            .WithMany()
            .HasForeignKey(x => x.LabTestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class LabTestResultConfiguration : IEntityTypeConfiguration<LabTestResult>
{
    public void Configure(EntityTypeBuilder<LabTestResult> builder)
    {
        builder.ToTable("LabTestResults", DatabaseSchemas.Lab);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ResultNumber).HasMaxLength(35).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.ParametersJson).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb").IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(2000);
        builder.Property(x => x.CriticalValueSummary).HasMaxLength(1000);
        builder.Property(x => x.AddendumNotes).HasMaxLength(2000);

        builder.HasIndex(x => x.ResultNumber).IsUnique();
        builder.HasIndex(x => x.LabSampleBookingId);
        builder.HasIndex(x => x.LabBookingItemId).IsUnique();
        builder.HasIndex(x => x.LabTestId);
        builder.HasIndex(x => new { x.Status, x.HasCriticalValue, x.IsAbnormal });
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.LabSampleBooking)
            .WithMany(x => x.Results)
            .HasForeignKey(x => x.LabSampleBookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.LabBookingItem)
            .WithMany()
            .HasForeignKey(x => x.LabBookingItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.LabTest)
            .WithMany()
            .HasForeignKey(x => x.LabTestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.EnteredByUser)
            .WithMany()
            .HasForeignKey(x => x.EnteredByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.TechnicianValidatedByUser)
            .WithMany()
            .HasForeignKey(x => x.TechnicianValidatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.ManagerValidatedByUser)
            .WithMany()
            .HasForeignKey(x => x.ManagerValidatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.ReleasedByUser)
            .WithMany()
            .HasForeignKey(x => x.ReleasedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.CriticalAlertAcknowledgedByUser)
            .WithMany()
            .HasForeignKey(x => x.CriticalAlertAcknowledgedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.AddendumByUser)
            .WithMany()
            .HasForeignKey(x => x.AddendumByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
