using Messenger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messenger.Infrastructure.Persistence.Configurations;

/// <summary>
/// Fluent API configuration for <see cref="MessageStatus"/>.
/// Composite PK: {message_id, user_id}.
/// </summary>
internal sealed class MessageStatusConfiguration : IEntityTypeConfiguration<MessageStatus>
{
    public void Configure(EntityTypeBuilder<MessageStatus> builder)
    {
        builder.ToTable("message_statuses");

        builder.HasKey(ms => new { ms.MessageId, ms.UserId });

        builder.Property(ms => ms.MessageId)
            .HasColumnName("message_id")
            .IsRequired();

        builder.Property(ms => ms.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(ms => ms.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(ms => ms.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(ms => ms.Message)
            .WithMany(m => m.Statuses)
            .HasForeignKey(ms => ms.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ms => ms.User)
            .WithMany(u => u.MessageStatuses)
            .HasForeignKey(ms => ms.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
