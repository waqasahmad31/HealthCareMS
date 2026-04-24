using HealthCareMS.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthCareMS.Infrastructure.Persistence.Configurations.Payments;

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("PaymentTransactions", DatabaseSchemas.Payment);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReferenceType).HasMaxLength(80).IsRequired();
        builder.Property(x => x.PaymentNumber).HasMaxLength(35).IsRequired();
        builder.Property(x => x.Gateway).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(12, 2);
        builder.Property(x => x.Currency).HasMaxLength(10).IsRequired();
        builder.Property(x => x.SessionToken).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CheckoutUrl).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ExternalReference).HasMaxLength(120);
        builder.Property(x => x.FailureCode).HasMaxLength(80);
        builder.Property(x => x.FailureMessage).HasMaxLength(500);
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb").IsRequired();
        builder.Property(x => x.LastWebhookPayload).HasColumnType("jsonb");

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.PaymentNumber).IsUnique();
        builder.HasIndex(x => x.SessionToken).IsUnique();
        builder.HasIndex(x => x.PharmacyOrderId);
        builder.HasIndex(x => new { x.ReferenceType, x.ReferenceId, x.Status });
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.PharmacyOrder)
            .WithMany()
            .HasForeignKey(x => x.PharmacyOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(x => x.Refunds)
            .WithOne(x => x.PaymentTransaction)
            .HasForeignKey(x => x.PaymentTransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PaymentInvoiceConfiguration : IEntityTypeConfiguration<PaymentInvoice>
{
    public void Configure(EntityTypeBuilder<PaymentInvoice> builder)
    {
        builder.ToTable("PaymentInvoices", DatabaseSchemas.Payment);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.InvoiceNumber).HasMaxLength(35).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.BillingName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.BillingEmail).HasMaxLength(255);
        builder.Property(x => x.BillingPhone).HasMaxLength(30);
        builder.Property(x => x.BillingAddress).HasMaxLength(1000);
        builder.Property(x => x.SubTotal).HasPrecision(12, 2);
        builder.Property(x => x.DeliveryFee).HasPrecision(12, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(12, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(12, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(12, 2);
        builder.Property(x => x.Currency).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.InvoiceNumber).IsUnique();
        builder.HasIndex(x => x.PaymentTransactionId).IsUnique();
        builder.HasIndex(x => x.PharmacyOrderId);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.PaymentTransaction)
            .WithOne(x => x.Invoice)
            .HasForeignKey<PaymentInvoice>(x => x.PaymentTransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.PharmacyOrder)
            .WithMany()
            .HasForeignKey(x => x.PharmacyOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(x => x.Refunds)
            .WithOne(x => x.PaymentInvoice)
            .HasForeignKey(x => x.PaymentInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PaymentRefundConfiguration : IEntityTypeConfiguration<PaymentRefund>
{
    public void Configure(EntityTypeBuilder<PaymentRefund> builder)
    {
        builder.ToTable("PaymentRefunds", DatabaseSchemas.Payment);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RefundNumber).HasMaxLength(35).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(12, 2);
        builder.Property(x => x.Currency).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ExternalReference).HasMaxLength(120);

        builder.HasIndex(x => x.RefundNumber).IsUnique();
        builder.HasIndex(x => x.PaymentTransactionId);
        builder.HasIndex(x => x.PaymentInvoiceId);
        builder.HasIndex(x => new { x.Status, x.RequestedAt });
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.PaymentTransaction)
            .WithMany(x => x.Refunds)
            .HasForeignKey(x => x.PaymentTransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.PaymentInvoice)
            .WithMany(x => x.Refunds)
            .HasForeignKey(x => x.PaymentInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.RequestedByUser)
            .WithMany()
            .HasForeignKey(x => x.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
