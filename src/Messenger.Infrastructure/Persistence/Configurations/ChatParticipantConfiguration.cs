using Messenger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messenger.Infrastructure.Persistence.Configurations;

/// <summary>
/// Fluent API configuration for the <see cref="ChatParticipant"/> join entity.
/// Composite PK: {chat_id, user_id}.
/// </summary>
internal sealed class ChatParticipantConfiguration : IEntityTypeConfiguration<ChatParticipant>
{
    public void Configure(EntityTypeBuilder<ChatParticipant> builder)
    {
        builder.ToTable("chat_participants");

        // Composite primary key
        builder.HasKey(cp => new { cp.ChatId, cp.UserId });

        builder.Property(cp => cp.ChatId)
            .HasColumnName("chat_id")
            .IsRequired();

        builder.Property(cp => cp.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(cp => cp.JoinedAt)
            .HasColumnName("joined_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Foreign keys
        builder.HasOne(cp => cp.Chat)
            .WithMany(c => c.Participants)
            .HasForeignKey(cp => cp.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cp => cp.User)
            .WithMany(u => u.ChatParticipants)
            .HasForeignKey(cp => cp.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
