using MediatR;
using Messenger.Application.Features.Chats.Queries.GetChatMessages;
using Messenger.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Application.Features.Search.Queries.SearchMessages;

public sealed class SearchMessagesQueryHandler : IRequestHandler<SearchMessagesQuery, List<MessageDto>>
{
    private readonly IApplicationDbContext _context;

    public SearchMessagesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<MessageDto>> Handle(SearchMessagesQuery request, CancellationToken cancellationToken)
    {
        var isParticipant = await _context.ChatParticipants
            .AnyAsync(p => p.ChatId == request.ChatId && p.UserId == request.UserId, cancellationToken);

        if (!isParticipant)
        {
            throw new UnauthorizedAccessException("You are not a member of this chat.");
        }

        var term = request.SearchTerm.ToLower();

        return await _context.Messages
            .Where(m => m.ChatId == request.ChatId && m.Content != null && m.Content.ToLower().Contains(term))
            .Join(_context.Users,
                  m => m.SenderId,
                  u => u.Id,
                  (m, u) => new { Message = m, User = u })
            .OrderByDescending(x => x.Message.CreatedAt)
            .Take(50)
            .Select(x => new MessageDto(
                x.Message.Id,
                x.Message.ChatId,
                x.Message.SenderId,
                x.User.FirstName,
                x.User.AvatarUrl,
                x.Message.Type,
                x.Message.Content,
                x.Message.MediaUrl,
                x.Message.Duration,
                x.Message.ReplyToMessageId,
                x.Message.CreatedAt,
                null)) // We don't load MyStatus for search results to keep it fast
            .ToListAsync(cancellationToken);
    }
}
