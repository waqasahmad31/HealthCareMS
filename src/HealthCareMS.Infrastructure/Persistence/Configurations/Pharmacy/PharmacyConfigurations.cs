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

public sealed class PrescriptionDispenseConfiguration : IEntityTypeConfiguration<PrescriptionDispense>
{
    public void Configure(EntityTypeBuilder<PrescriptionDispense> builder)
    {
        builder.ToTable("PrescriptionDispenses", DatabaseSchemas.Pharmacy);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DispenseNumber).HasMaxLength(35).IsRequired();
        builder.Property(x => x.ReceiptNumber).HasMaxLength(35).IsRequired();
        builder.Property(x => x.VerificationCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.SubTotal).HasPrecision(12, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(12, 2);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.DispenseNumber).IsUnique();
        builder.HasIndex(x => x.ReceiptNumber).IsUnique();
        builder.HasIndex(x => new { x.PrescriptionId, x.Status });
        builder.HasIndex(x => new { x.PatientId, x.DispensedAt });
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Prescription)
            .WithMany()
            .HasForeignKey(x => x.PrescriptionId)
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

public sealed class PrescriptionDispenseItemConfiguration : IEntityTypeConfiguration<PrescriptionDispenseItem>
{
    public void Configure(EntityTypeBuilder<PrescriptionDispenseItem> builder)
    {
        builder.ToTable("PrescriptionDispenseItems", DatabaseSchemas.Pharmacy);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PrescribedMedicineName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DispensedMedicineName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.QuantityPrescribed).HasPrecision(10, 2);
        builder.Property(x => x.UnitPrice).HasPrecision(12, 2);
        builder.Property(x => x.LineTotal).HasPrecision(12, 2);

        builder.HasIndex(x => new { x.PrescriptionDispenseId, x.PrescriptionItemId }).IsUnique();
        builder.HasIndex(x => x.MedicineId);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.PrescriptionDispense)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.PrescriptionDispenseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.PrescriptionItem)
            .WithMany()
            .HasForeignKey(x => x.PrescriptionItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Medicine)
            .WithMany()
            .HasForeignKey(x => x.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PrescriptionDispenseBatchConfiguration : IEntityTypeConfiguration<PrescriptionDispenseBatch>
{
    public void Configure(EntityTypeBuilder<PrescriptionDispenseBatch> builder)
    {
        builder.ToTable("PrescriptionDispenseBatches", DatabaseSchemas.Pharmacy);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BatchNumber).HasMaxLength(100).IsRequired();

        builder.HasIndex(x => new { x.PrescriptionDispenseItemId, x.StockBatchId });
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.PrescriptionDispenseItem)
            .WithMany(x => x.Batches)
            .HasForeignKey(x => x.PrescriptionDispenseItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.StockBatch)
            .WithMany()
            .HasForeignKey(x => x.StockBatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PharmacyOrderConfiguration : IEntityTypeConfiguration<PharmacyOrder>
{
    public void Configure(EntityTypeBuilder<PharmacyOrder> builder)
    {
        builder.ToTable("PharmacyOrders", DatabaseSchemas.Pharmacy);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderNumber).HasMaxLength(35).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.DeliveryAddress).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.PrescriptionUploadFileName).HasMaxLength(255);
        builder.Property(x => x.PrescriptionUploadContentType).HasMaxLength(120);
        builder.Property(x => x.PatientNotes).HasMaxLength(1000);
        builder.Property(x => x.PharmacistNotes).HasMaxLength(1000);
        builder.Property(x => x.SubTotal).HasPrecision(12, 2);
        builder.Property(x => x.DeliveryFee).HasPrecision(12, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(12, 2);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.OrderNumber).IsUnique();
        builder.HasIndex(x => new { x.PatientId, x.OrderedAt });
        builder.HasIndex(x => new { x.Status, x.OrderedAt });
        builder.HasIndex(x => x.DeliveryAgentUserId);
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
            .HasOne(x => x.Prescription)
            .WithMany()
            .HasForeignKey(x => x.PrescriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.DeliveryAgentUser)
            .WithMany()
            .HasForeignKey(x => x.DeliveryAgentUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PharmacyOrderItemConfiguration : IEntityTypeConfiguration<PharmacyOrderItem>
{
    public void Configure(EntityTypeBuilder<PharmacyOrderItem> builder)
    {
        builder.ToTable("PharmacyOrderItems", DatabaseSchemas.Pharmacy);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MedicineName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.UnitPrice).HasPrecision(12, 2);
        builder.Property(x => x.LineTotal).HasPrecision(12, 2);

        builder.HasIndex(x => new { x.PharmacyOrderId, x.MedicineId }).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.PharmacyOrder)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.PharmacyOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Medicine)
            .WithMany()
            .HasForeignKey(x => x.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
