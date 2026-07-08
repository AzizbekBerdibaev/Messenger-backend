using Messenger.Domain.Entities;
using Messenger.Domain.Interfaces;
using Messenger.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext. Implements <see cref="IApplicationDbContext"/> so Application
/// handlers can depend on the abstraction rather than the concrete class.
/// All entity configurations live in separate <c>IEntityTypeConfiguration&lt;T&gt;</c> classes.
/// </summary>
public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<ChatParticipant> ChatParticipants => Set<ChatParticipant>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageStatus> MessageStatuses => Set<MessageStatus>();
    public DbSet<Story> Stories => Set<Story>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();
    public DbSet<BlockedUser> BlockedUsers => Set<BlockedUser>();
    public DbSet<Contact> Contacts => Set<Contact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> implementations in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
