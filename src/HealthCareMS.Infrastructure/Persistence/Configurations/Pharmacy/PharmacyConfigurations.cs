using HealthCareMS.Domain.Pharmacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthCareMS.Infrastructure.Persistence.Configurations.Pharmacy;

public sealed class MedicineConfiguration : IEntityTypeConfiguration<Medicine>
{
    public void Configure(EntityTypeBuilder<Medicine> builder)
    {
        builder.ToTable("Medicines", DatabaseSchemas.Pharmacy);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.GenericName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.BrandName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.DosageForm).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Strength).HasMaxLength(80);
        builder.Property(x => x.DrapRegistrationNumber).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Manufacturer).HasMaxLength(180);
        builder.Property(x => x.UnitPrice).HasPrecision(12, 2);
        builder.Property(x => x.UnitCostPrice).HasPrecision(12, 2);
        builder.Property(x => x.Barcode).HasMaxLength(120).IsRequired();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.BrandName, x.GenericName });
        builder.HasIndex(x => x.Barcode).IsUnique();
        builder.HasIndex(x => x.DrapRegistrationNumber);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StockBatchConfiguration : IEntityTypeConfiguration<StockBatch>
{
    public void Configure(EntityTypeBuilder<StockBatch> builder)
    {
        builder.ToTable("StockBatches", DatabaseSchemas.Pharmacy);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BatchNumber).HasMaxLength(100).IsRequired();
        builder.Property(x => x.UnitCostPrice).HasPrecision(12, 2);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.MedicineId, x.BatchNumber }).IsUnique();
        builder.HasIndex(x => x.ExpiryDate);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Medicine)
            .WithMany(x => x.StockBatches)
            .HasForeignKey(x => x.MedicineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers", DatabaseSchemas.Pharmacy);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ContactPerson).HasMaxLength(160);
        builder.Property(x => x.Phone).HasMaxLength(30);
        builder.Property(x => x.Email).HasMaxLength(254);
        builder.Property(x => x.Address).HasMaxLength(500);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.Name);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.ToTable("StockAdjustments", DatabaseSchemas.Pharmacy);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AdjustmentType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.MedicineId, x.AdjustedAt });
        builder.HasIndex(x => new { x.StockBatchId, x.AdjustedAt });
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Medicine)
            .WithMany()
            .HasForeignKey(x => x.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.StockBatch)
            .WithMany()
            .HasForeignKey(x => x.StockBatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StockAlertConfiguration : IEntityTypeConfiguration<StockAlert>
{
    public void Configure(EntityTypeBuilder<StockAlert> builder)
    {
        builder.ToTable("StockAlerts", DatabaseSchemas.Pharmacy);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AlertType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Severity).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(1000).IsRequired();

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.MedicineId, x.AlertType, x.Status });
        builder.HasIndex(x => new { x.StockBatchId, x.AlertType, x.Status });
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Medicine)
            .WithMany()
            .HasForeignKey(x => x.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.StockBatch)
            .WithMany()
            .HasForeignKey(x => x.StockBatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
