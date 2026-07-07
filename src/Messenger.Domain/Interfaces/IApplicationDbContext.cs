using Messenger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Domain.Interfaces;

/// <summary>
/// Abstraction over the EF Core DbContext. Defined in Domain so that the Application
/// layer can depend on it without referencing Infrastructure.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User>            Users            { get; }
    DbSet<Chat>            Chats            { get; }
    DbSet<ChatParticipant> ChatParticipants { get; }
    DbSet<Message>         Messages         { get; }
    DbSet<MessageStatus>   MessageStatuses  { get; }
    DbSet<Story>           Stories          { get; }
    DbSet<UserDevice>      UserDevices      { get; }
    DbSet<BlockedUser>     BlockedUsers     { get; }
    DbSet<Contact>         Contacts         { get; }

    /// <summary>Persists all pending changes to the database.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
