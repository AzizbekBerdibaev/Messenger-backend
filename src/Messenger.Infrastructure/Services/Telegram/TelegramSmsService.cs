using Messenger.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Telegram.Bot;

namespace Messenger.Infrastructure.Services.Telegram;

public class TelegramSmsService : ISmsService
{
    private readonly ILogger<TelegramSmsService> _logger;
    private readonly ITelegramBotClient _botClient;
    private readonly IConnectionMultiplexer _redis;

    public TelegramSmsService(
        ILogger<TelegramSmsService> logger, 
        ITelegramBotClient botClient,
        IConnectionMultiplexer redis)
    {
        _logger = logger;
        _botClient = botClient;
        _redis = redis;
    }

    public async Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = $"telegram_chat:{phoneNumber}";
        
        var chatIdValue = await db.StringGetAsync(key);
        
        if (!chatIdValue.HasValue)
        {
            _logger.LogWarning("Cannot send OTP. User with phone {Phone} is not registered in Telegram bot.", phoneNumber);
            // In a real app we might throw a specific exception to inform the user
            // "Please start the bot @MyMessengerAuthBot and share your contact."
            // For now we will just log and return to allow tests/development to proceed if needed,
            // or we could throw. Let's throw a business exception or standard invalid operation.
            throw new InvalidOperationException($"User not registered in Telegram bot. Please start the bot and share your contact first.");
        }

        long chatId = long.Parse(chatIdValue!);

        _logger.LogInformation("Sending Telegram message to ChatId {ChatId} for Phone {Phone}", chatId, phoneNumber);

        await _botClient.SendMessage(
            chatId: chatId,
            text: message,
            cancellationToken: cancellationToken);
    }
}
