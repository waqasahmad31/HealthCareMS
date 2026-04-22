using HealthCareMS.Domain.Consultations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthCareMS.Infrastructure.Persistence.Configurations.Consultations;

public sealed class DrapMedicineConfiguration : IEntityTypeConfiguration<DrapMedicine>
{
    public void Configure(EntityTypeBuilder<DrapMedicine> builder)
    {
        builder.ToTable("DrapMedicines", DatabaseSchemas.Consultation);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DrapRegistrationNumber).HasMaxLength(80).IsRequired();
        builder.Property(x => x.BrandName).HasMaxLength(180).IsRequired();
        builder.Property(x => x.GenericName).HasMaxLength(180).IsRequired();
        builder.Property(x => x.Strength).HasMaxLength(80);
        builder.Property(x => x.DosageForm).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Manufacturer).HasMaxLength(180);
        builder.Property(x => x.AllergenKeywords).HasMaxLength(600).IsRequired();

        builder.HasIndex(x => x.DrapRegistrationNumber).IsUnique();
        builder.HasIndex(x => new { x.BrandName, x.GenericName });
        builder.HasIndex(x => x.IsBanned);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
