using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Messenger.Infrastructure.Persistence;
using StackExchange.Redis;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Messenger.Infrastructure.Services.Telegram;

public class TelegramBotListener : BackgroundService
{
    private readonly ILogger<TelegramBotListener> _logger;
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceProvider _serviceProvider;

    public TelegramBotListener(
        ILogger<TelegramBotListener> logger,
        ITelegramBotClient botClient,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _botClient = botClient;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message }
        };

        _logger.LogInformation("Starting Telegram bot polling with auto-reconnect...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

                _botClient.StartReceiving(
                    updateHandler: HandleUpdateAsync,
                    errorHandler: HandlePollingErrorAsync,
                    receiverOptions: receiverOptions,
                    cancellationToken: linkedCts.Token
                );

                _logger.LogInformation("Telegram bot polling started successfully.");

                // Wait until service shutdown
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Telegram bot polling stopped due to service shutdown.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telegram bot polling encountered an error. Reconnecting in 5 seconds...");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message)
            return;
            
        var chatId = message.Chat.Id;

        // User clicked Start or sent a message
        if (message.Text == "/start")
        {
            var replyKeyboardMarkup = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton("📞 Поделиться контактом (Share Contact)") { RequestContact = true }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };

            await botClient.SendMessage(
                chatId: chatId,
                text: "Добро пожаловать в Messenger! Для того чтобы получать коды авторизации (OTP), пожалуйста, поделитесь своим контактом, нажав кнопку ниже.",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
            
            return;
        }

        // Handle contact sharing
        if (message.Type == MessageType.Contact && message.Contact is not null)
        {
            var phone = message.Contact.PhoneNumber;
            
            // Normalize phone (sometimes Telegram includes + and sometimes not)
            if (!phone.StartsWith("+"))
                phone = "+" + phone;

            _logger.LogInformation("Received contact sharing from phone: {Phone} with ChatId: {ChatId}", phone, chatId);

            // 1. Save to Redis (fast cache)
            using var scope = _serviceProvider.CreateScope();
            try
            {
                var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
                var db = redis.GetDatabase();
                var key = $"telegram_chat:{phone}";
                await db.StringSetAsync(key, chatId.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache Telegram chat ID in Redis for phone {Phone}", phone);
            }

            // 2. Save to PostgreSQL (permanent persistence)
            try
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await dbContext.Database.ExecuteSqlRawAsync(
                    @"INSERT INTO telegram_bindings (phone_number, chat_id, updated_at) 
                      VALUES ({0}, {1}, {2}) 
                      ON CONFLICT (phone_number) 
                      DO UPDATE SET chat_id = EXCLUDED.chat_id, updated_at = EXCLUDED.updated_at;",
                    phone, chatId, DateTime.UtcNow);
                _logger.LogInformation("Saved Telegram binding in PostgreSQL for {Phone} -> {ChatId}", phone, chatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist Telegram chat ID in PostgreSQL for phone {Phone}", phone);
            }

            var replyKeyboardMarkup = new ReplyKeyboardRemove();

            await botClient.SendMessage(
                chatId: chatId,
                text: $"Спасибо! Ваш номер ({phone}) привязан. Теперь вы можете запрашивать код авторизации в приложении.",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
        }
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogError("Telegram polling error: {ErrorMessage}", ErrorMessage);
        return Task.CompletedTask;
    }
}
