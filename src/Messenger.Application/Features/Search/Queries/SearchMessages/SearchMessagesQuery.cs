using MediatR;
using Messenger.Application.Features.Chats.Queries.GetChatMessages;

namespace Messenger.Application.Features.Search.Queries.SearchMessages;

public sealed record SearchMessagesQuery(Guid ChatId, Guid UserId, string SearchTerm) : IRequest<List<MessageDto>>;
