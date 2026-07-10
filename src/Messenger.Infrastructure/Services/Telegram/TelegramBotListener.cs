using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
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
        // Setup receiver options to only receive Message updates
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message }
        };

        _logger.LogInformation("Starting Telegram bot polling...");

        try
        {
            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: stoppingToken
            );

            // Wait forever
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telegram bot polling failed.");
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

            // Save to Redis
            using var scope = _serviceProvider.CreateScope();
            var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
            var db = redis.GetDatabase();

            var key = $"telegram_chat:{phone}";
            // Store mapping forever (or a very long time)
            await db.StringSetAsync(key, chatId.ToString());

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
