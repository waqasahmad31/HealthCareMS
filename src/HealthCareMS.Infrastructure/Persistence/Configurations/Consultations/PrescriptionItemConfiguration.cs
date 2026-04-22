using HealthCareMS.Domain.Consultations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthCareMS.Infrastructure.Persistence.Configurations.Consultations;

public sealed class PrescriptionItemConfiguration : IEntityTypeConfiguration<PrescriptionItem>
{
    public void Configure(EntityTypeBuilder<PrescriptionItem> builder)
    {
        builder.ToTable("PrescriptionItems", DatabaseSchemas.Consultation);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MedicineName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.GenericName).HasMaxLength(200);
        builder.Property(x => x.Strength).HasMaxLength(80);
        builder.Property(x => x.Route).HasMaxLength(80);
        builder.Property(x => x.Dosage).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Frequency).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(10, 2);
        builder.Property(x => x.Instructions).HasMaxLength(1000);

        builder.HasIndex(x => new { x.PrescriptionId, x.SortOrder });
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.Prescription)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
