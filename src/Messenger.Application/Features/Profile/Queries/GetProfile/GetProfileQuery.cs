using MediatR;

namespace Messenger.Application.Features.Profile.Queries.GetProfile;

public sealed record GetProfileQuery(Guid UserId) : IRequest<ProfileDto>;

public sealed record ProfileDto(
    Guid UserId,
    string PhoneNumber,
    string FirstName,
    string? LastName,
    string? AvatarUrl);
