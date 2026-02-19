using HabiBot.Application.Services;
using HabiBot.Bot.StateManagement;
using HabiBot.Infrastructure.Models.Telegram;
using HabiBot.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HabiBot.Bot.Commands;

/// <summary>
/// Команда /summary - показать ежедневную сводку выполнения привычек
/// </summary>
public class SummaryCommand : BotCommandBase
{
    private readonly IUserService _userService;
    private readonly IDailySummaryService _dailySummaryService;

    public SummaryCommand(
        ITelegramApiClient telegramClient,
        IUserStateManager stateManager,
        IUserService userService,
        IDailySummaryService dailySummaryService,
        ILogger<SummaryCommand> logger)
        : base(telegramClient, stateManager, logger)
    {
        _userService = userService;
        _dailySummaryService = dailySummaryService;
    }

    public override string Name => "summary";
    public override string Description => "Показать ежедневную сводку";

    public override async Task ExecuteAsync(Update update, CancellationToken cancellationToken = default)
    {
        var chatId = GetChatId(update);
        var userId = GetUserId(update);

        if (chatId == null || userId == null)
        {
            Logger.LogWarning("Получен Update без ChatId или UserId");
            return;
        }

        try
        {
            // Проверяем регистрацию
            var user = await _userService.GetByTelegramIdAsync(userId.Value, cancellationToken);
            
            if (user == null)
            {
                await SendMessageAsync(chatId.Value, 
                    "Ты не зарегистрирован. Используй /start для регистрации.", 
                    cancellationToken);
                return;
            }

            // Получаем сводку за сегодня
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var summaryText = await _dailySummaryService.GenerateSummaryTextAsync(
                user.Id, 
                today, 
                includeNextDay: false, 
                cancellationToken);

            // Отправляем сводку с HTML-разметкой
            await SendHtmlMessageAsync(chatId.Value, summaryText, cancellationToken);

            Logger.LogInformation("Отправлена сводка для пользователя {UserId} за дату {Date}", 
                userId.Value, today);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка при получении сводки для пользователя {UserId}", userId);
            await SendMessageAsync(chatId.Value, 
                "Произошла ошибка при получении сводки.\n\nПожалуйста, попробуйте позже.", 
                cancellationToken);
        }
    }

    /// <summary>
    /// Отправить сообщение с HTML-разметкой
    /// </summary>
    private async Task SendHtmlMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new SendMessageRequest
            {
                ChatId = chatId,
                Text = text,
                ParseMode = "HTML"
            };
            await TelegramClient.SendMessageAsync(request, cancellationToken);
            Logger.LogDebug("Отправлено HTML-сообщение в чат {ChatId}", chatId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка отправки HTML-сообщения в чат {ChatId}", chatId);
            throw;
        }
    }
}
