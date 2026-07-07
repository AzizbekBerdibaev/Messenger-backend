using Messenger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messenger.Infrastructure.Persistence.Configurations;

public class UserDeviceConfiguration : IEntityTypeConfiguration<UserDevice>
{
    public void Configure(EntityTypeBuilder<UserDevice> builder)
    {
        builder.ToTable("UserDevices");
        
        // Composite PK because a user could have multiple devices
        builder.HasKey(d => new { d.UserId, d.DeviceToken });

        builder.Property(d => d.DeviceToken)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(d => d.DeviceType)
            .IsRequired();

        // One-to-Many: User -> UserDevices
        builder.HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
