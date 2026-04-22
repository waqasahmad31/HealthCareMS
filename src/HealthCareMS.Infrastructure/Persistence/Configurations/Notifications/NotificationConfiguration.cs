using HealthCareMS.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthCareMS.Infrastructure.Persistence.Configurations.Notifications;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications", DatabaseSchemas.Notification);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).HasDefaultValue(NotificationStatus.Pending).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Destination).HasMaxLength(255);
        builder.Property(x => x.ReferenceType).HasMaxLength(80);
        builder.Property(x => x.FailureReason).HasMaxLength(1000);

        builder.HasIndex(x => new { x.RecipientUserId, x.IsRead, x.CreatedAt });
        builder.HasIndex(x => new { x.ReferenceType, x.ReferenceId });
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.RecipientUser)
            .WithMany()
            .HasForeignKey(x => x.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

