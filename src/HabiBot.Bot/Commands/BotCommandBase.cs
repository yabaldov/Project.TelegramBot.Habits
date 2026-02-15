using HabiBot.Bot.StateManagement;
using HabiBot.Infrastructure.Models.Telegram;
using HabiBot.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HabiBot.Bot.Commands;

/// <summary>
/// Базовый класс для команд бота
/// </summary>
public abstract class BotCommandBase : IBotCommand
{
    protected readonly ITelegramApiClient TelegramClient;
    protected readonly IUserStateManager StateManager;
    protected readonly ILogger Logger;

    protected BotCommandBase(
        ITelegramApiClient telegramClient,
        IUserStateManager stateManager,
        ILogger logger)
    {
        TelegramClient = telegramClient;
        StateManager = stateManager;
        Logger = logger;
    }

    public abstract string Name { get; }
    public abstract string Description { get; }

    public abstract Task ExecuteAsync(Update update, CancellationToken cancellationToken = default);

    /// <summary>
    /// Отправить текстовое сообщение пользователю
    /// </summary>
    protected async Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new SendMessageRequest
            {
                ChatId = chatId,
                Text = text
            };
            await TelegramClient.SendMessageAsync(request, cancellationToken);
            Logger.LogDebug("Отправлено сообщение в чат {ChatId}: {Text}", chatId, text);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка отправки сообщения в чат {ChatId}", chatId);
            throw;
        }
    }

    /// <summary>
    /// Получить текст сообщения из Update
    /// </summary>
    protected static string? GetMessageText(Update update)
    {
        return update.Message?.Text;
    }

    /// <summary>
    /// Получить Chat ID из Update
    /// </summary>
    protected static long? GetChatId(Update update)
    {
        return update.Message?.Chat.Id;
    }

    /// <summary>
    /// Получить Telegram ID пользователя из Update
    /// </summary>
    protected static long? GetUserId(Update update)
    {
        return update.Message?.From?.Id;
    }
}
