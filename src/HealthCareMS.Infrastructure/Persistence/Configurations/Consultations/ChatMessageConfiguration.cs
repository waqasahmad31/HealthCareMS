using HealthCareMS.Domain.Consultations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthCareMS.Infrastructure.Persistence.Configurations.Consultations;

public sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("ChatMessages", DatabaseSchemas.Consultation);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SenderType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.SenderDisplayName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.MessageType).HasConversion<string>().HasMaxLength(20).HasDefaultValue(ChatMessageType.Text).IsRequired();
        builder.Property(x => x.MessageText).HasMaxLength(4000);
        builder.Property(x => x.AttachmentFileName).HasMaxLength(260);
        builder.Property(x => x.AttachmentContentType).HasMaxLength(120);
        builder.Property(x => x.AttachmentStoragePath).HasMaxLength(600);

        builder.HasIndex(x => new { x.SessionId, x.SentAt });
        builder.HasIndex(x => new { x.SessionId, x.SenderType });
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder
            .HasOne(x => x.Session)
            .WithMany()
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
