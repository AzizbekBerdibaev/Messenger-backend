using System.Data;
using Messenger.Application.Common.Interfaces;
using Messenger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Telegram.Bot;

namespace Messenger.Infrastructure.Services.Telegram;

public class TelegramSmsService : ISmsService
{
    private readonly ILogger<TelegramSmsService> _logger;
    private readonly ITelegramBotClient _botClient;
    private readonly IConnectionMultiplexer _redis;
    private readonly ApplicationDbContext _dbContext;

    public TelegramSmsService(
        ILogger<TelegramSmsService> logger, 
        ITelegramBotClient botClient,
        IConnectionMultiplexer redis,
        ApplicationDbContext dbContext)
    {
        _logger = logger;
        _botClient = botClient;
        _redis = redis;
        _dbContext = dbContext;
    }

    public async Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        long? chatId = null;

        // 1. Try Redis cache first (1ms)
        try
        {
            var db = _redis.GetDatabase();
            var key = $"telegram_chat:{phoneNumber}";
            var chatIdValue = await db.StringGetAsync(key);
            
            if (chatIdValue.HasValue && long.TryParse(chatIdValue!, out var parsedId))
            {
                chatId = parsedId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read Telegram chat ID from Redis for {Phone}. Falling back to PostgreSQL.", phoneNumber);
        }

        // 2. If not in Redis, fallback to PostgreSQL (permanent storage)
        if (chatId is null)
        {
            _logger.LogInformation("Looking up Telegram binding in PostgreSQL for {Phone}...", phoneNumber);
            
            using var connection = _dbContext.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT chat_id FROM telegram_bindings WHERE phone_number = @phone LIMIT 1";
            var param = cmd.CreateParameter();
            param.ParameterName = "@phone";
            param.Value = phoneNumber;
            cmd.Parameters.Add(param);

            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            if (result is not null && result != DBNull.Value)
            {
                chatId = Convert.ToInt64(result);

                // Re-cache into Redis so next lookup is instant
                try
                {
                    var db = _redis.GetDatabase();
                    await db.StringSetAsync($"telegram_chat:{phoneNumber}", chatId.Value.ToString());
                }
                catch { }
            }
        }

        // 3. If user has never started the bot or shared contact
        if (chatId is null)
        {
            _logger.LogWarning("Cannot send OTP. User with phone {Phone} is not registered in Telegram bot.", phoneNumber);
            throw new InvalidOperationException($"User not registered in Telegram bot. Please start the bot and share your contact first.");
        }

        _logger.LogInformation("Sending Telegram message to ChatId {ChatId} for Phone {Phone}", chatId, phoneNumber);

        await _botClient.SendMessage(
            chatId: chatId.Value,
            text: message,
            cancellationToken: cancellationToken);
    }
}
