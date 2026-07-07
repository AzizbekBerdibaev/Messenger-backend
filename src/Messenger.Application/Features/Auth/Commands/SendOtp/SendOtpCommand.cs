using MediatR;
using Messenger.Application.Common.Interfaces;

namespace Messenger.Application.Features.Auth.Commands.SendOtp;

/// <summary>
/// Command: Sends a 4-digit OTP to the specified phone number.
/// The OTP is stored in Redis with a 5-minute TTL.
/// On success returns <c>Unit</c>; the SMS is dispatched via <see cref="ISmsService"/>.
/// </summary>
public sealed record SendOtpCommand(string PhoneNumber) : IRequest<Unit>;
