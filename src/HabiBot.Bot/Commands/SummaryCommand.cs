using HabiBot.Application.DTOs;
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
    private readonly IHabitLogService _habitLogService;
    private readonly IHabitService _habitService;

    public SummaryCommand(
        ITelegramApiClient telegramClient,
        IUserStateManager stateManager,
        IUserService userService,
        IDailySummaryService dailySummaryService,
        IHabitLogService habitLogService,
        IHabitService habitService,
        ILogger<SummaryCommand> logger)
        : base(telegramClient, stateManager, logger)
    {
        _userService = userService;
        _dailySummaryService = dailySummaryService;
        _habitLogService = habitLogService;
        _habitService = habitService;
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
                    cancellationToken,
                    replyMarkup: ReplyKeyboardHelper.PreRegistrationKeyboard);
                return;
            }

            // Получаем сводку за сегодня
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var summary = await _dailySummaryService.GetDailySummaryAsync(
                user.Id, today, cancellationToken);

            var summaryText = await _dailySummaryService.GenerateSummaryTextAsync(
                user.Id, 
                today, 
                includeNextDay: false, 
                cancellationToken);

            // Генерируем inline keyboard для невыполненных привычек
            var keyboard = BuildUncompletedHabitsKeyboard(summary);

            // Отправляем сводку с HTML-разметкой и кнопками
            await SendHtmlMessageAsync(chatId.Value, summaryText, cancellationToken, keyboard);

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
    /// Обработка callback при нажатии кнопки "Отметить выполненной" из сводки
    /// </summary>
    public async Task HandleCompleteCallbackAsync(long chatId, long userId, long habitId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Проверяем, что пользователь зарегистрирован
            var user = await _userService.GetByTelegramIdAsync(userId, cancellationToken);
            if (user == null)
            {
                await SendMessageAsync(chatId, "Пользователь не найден.", cancellationToken);
                return;
            }

            // Проверяем, что привычка существует и принадлежит пользователю
            var habit = await _habitService.GetByIdAsync(habitId, cancellationToken);
            if (habit == null || habit.UserId != user.Id)
            {
                await SendMessageAsync(chatId, "Привычка не найдена. 🤔", cancellationToken);
                return;
            }

            // Создаём запись о выполнении
            var createLogDto = new CreateHabitLogDto
            {
                HabitId = habitId,
                CompletedAt = DateTime.UtcNow
            };
            await _habitLogService.CreateLogAsync(createLogDto, cancellationToken);

            await SendMessageAsync(chatId,
                $"Отлично! 🎉 Привычка \"{habit.Name}\" отмечена как выполненная.\n\nТак держать! 💪",
                cancellationToken);

            Logger.LogInformation("Привычка {HabitId} отмечена из сводки для пользователя {UserId}", habitId, userId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка отметки привычки {HabitId} из сводки для пользователя {UserId}", habitId, userId);
            await SendMessageAsync(chatId, "Произошла ошибка при отметке привычки.", cancellationToken);
        }
    }

    /// <summary>
    /// Построить inline keyboard из невыполненных привычек
    /// </summary>
    public static InlineKeyboardMarkup? BuildUncompletedHabitsKeyboard(DailySummaryData summary)
    {
        return SummaryKeyboardBuilder.BuildUncompletedHabitsKeyboard(summary);
    }

    /// <summary>
    /// Отправить сообщение с HTML-разметкой
    /// </summary>
    private async Task SendHtmlMessageAsync(long chatId, string text, CancellationToken cancellationToken = default, object? replyMarkup = null)
    {
        try
        {
            var request = new SendMessageRequest
            {
                ChatId = chatId,
                Text = text,
                ParseMode = "HTML",
                ReplyMarkup = replyMarkup
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
