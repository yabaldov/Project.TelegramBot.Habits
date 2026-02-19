using HabiBot.Application.Services;
using HabiBot.Bot.StateManagement;
using HabiBot.Infrastructure.Models.Telegram;
using HabiBot.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace HabiBot.Bot.Commands;

/// <summary>
/// Команда /setsummary - настройка ежедневной сводки
/// </summary>
public class SetSummaryCommand : BotCommandBase
{
    private readonly IUserService _userService;

    public SetSummaryCommand(
        ITelegramApiClient telegramClient,
        IUserStateManager stateManager,
        IUserService userService,
        ILogger<SetSummaryCommand> logger)
        : base(telegramClient, stateManager, logger)
    {
        _userService = userService;
    }

    public override string Name => "setsummary";
    public override string Description => "Настроить ежедневную сводку";

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

            // Сохраняем ID пользователя в контексте
            StateManager.SetData(userId.Value, "UserId", user.Id);

            // Показываем текущие настройки и кнопки
            var statusText = user.IsDailySummaryEnabled ? "включена ✅" : "отключена ❌";
            var timeText = user.DailySummaryTime?.ToString(@"hh\:mm") ?? "21:00";

            var buttons = new List<List<InlineKeyboardButton>>();

            if (user.IsDailySummaryEnabled)
            {
                buttons.Add(new List<InlineKeyboardButton>
                {
                    new() { Text = "⏰ Изменить время", CallbackData = "setsummary:time" }
                });
                buttons.Add(new List<InlineKeyboardButton>
                {
                    new() { Text = "❌ Отключить сводку", CallbackData = "setsummary:disable" }
                });
            }
            else
            {
                buttons.Add(new List<InlineKeyboardButton>
                {
                    new() { Text = "✅ Включить сводку", CallbackData = "setsummary:enable" }
                });
            }

            buttons.Add(new List<InlineKeyboardButton>
            {
                new() { Text = "↩️ Отмена", CallbackData = "setsummary:cancel" }
            });

            var keyboard = new InlineKeyboardMarkup { InlineKeyboard = buttons };

            await SendMessageAsync(chatId.Value,
                $"⚙️ Настройки ежедневной сводки\n\n" +
                $"Статус: {statusText}\n" +
                $"Время отправки: {timeText}\n\n" +
                "Выбери действие:",
                cancellationToken,
                keyboard);

            Logger.LogInformation("Показаны настройки сводки для пользователя {UserId}", userId.Value);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка выполнения команды /setsummary для пользователя {UserId}", userId);
            await SendMessageAsync(chatId.Value,
                "Произошла ошибка. Попробуй позже.",
                cancellationToken);
        }
    }

    /// <summary>
    /// Обработка выбора действия через inline кнопки
    /// </summary>
    public async Task HandleCallbackAsync(long chatId, long userId, string action, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userService.GetByTelegramIdAsync(userId, cancellationToken);
            if (user == null)
            {
                await SendMessageAsync(chatId, "Пользователь не найден.", cancellationToken);
                return;
            }

            switch (action)
            {
                case "time":
                    await SendMessageAsync(chatId,
                        "Введи время для ежедневной сводки в формате ЧЧ:ММ (например: 21:00 или 08:30):",
                        cancellationToken);
                    StateManager.SetData(userId, "UserId", user.Id);
                    StateManager.SetState(userId, UserState.WaitingForSummaryTime);
                    break;

                case "disable":
                    await _userService.UpdateDailySummarySettingsAsync(user.Id, isEnabled: false, cancellationToken: cancellationToken);
                    await SendMessageAsync(chatId,
                        "Ежедневная сводка отключена. ❌\n\n" +
                        "Ты всегда можешь включить её снова через /setsummary.\n" +
                        "Также ты можешь посмотреть сводку в любое время через /summary.",
                        cancellationToken);
                    Logger.LogInformation("Сводка отключена для пользователя {UserId}", userId);
                    break;

                case "enable":
                    var currentTime = user.DailySummaryTime ?? new TimeSpan(21, 0, 0);
                    await _userService.UpdateDailySummarySettingsAsync(user.Id, isEnabled: true, summaryTime: currentTime, cancellationToken: cancellationToken);
                    var timeStr = currentTime.ToString(@"hh\:mm");
                    await SendMessageAsync(chatId,
                        $"Ежедневная сводка включена! ✅\n\n" +
                        $"Я буду присылать тебе сводку ежедневно в {timeStr}.\n" +
                        "Изменить время можно через /setsummary.",
                        cancellationToken);
                    Logger.LogInformation("Сводка включена для пользователя {UserId} в {Time}", userId, timeStr);
                    break;

                case "cancel":
                    await SendMessageAsync(chatId, "Настройка сводки отменена.", cancellationToken);
                    StateManager.ClearState(userId);
                    break;

                default:
                    Logger.LogWarning("Неизвестное действие setsummary: {Action}", action);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка обработки callback setsummary для пользователя {UserId}", userId);
            await SendMessageAsync(chatId, "Произошла ошибка. Попробуй позже.", cancellationToken);
        }
    }

    /// <summary>
    /// Обработка ввода времени для сводки
    /// </summary>
    public async Task HandleSummaryTimeInputAsync(long chatId, long userId, string timeText, CancellationToken cancellationToken = default)
    {
        try
        {
            // Валидация формата времени (HH:mm)
            var timeRegex = new Regex(@"^([0-1]?[0-9]|2[0-3]):([0-5][0-9])$");
            var match = timeRegex.Match(timeText.Trim());

            if (!match.Success)
            {
                await SendMessageAsync(chatId,
                    "Неверный формат времени. ⚠️\n\n" +
                    "Используй формат ЧЧ:ММ (например: 21:00 или 08:30):",
                    cancellationToken);
                return;
            }

            // Нормализуем формат
            var hours = int.Parse(match.Groups[1].Value);
            var minutes = int.Parse(match.Groups[2].Value);
            var summaryTime = new TimeSpan(hours, minutes, 0);

            // Получаем ID пользователя из контекста
            var userIdDb = StateManager.GetData<long>(userId, "UserId");
            if (userIdDb == 0)
            {
                await SendMessageAsync(chatId,
                    "Произошла ошибка. Попробуй начать заново с /setsummary",
                    cancellationToken);
                StateManager.ClearState(userId);
                return;
            }

            // Обновляем настройки
            await _userService.UpdateDailySummarySettingsAsync(userIdDb, isEnabled: true, summaryTime: summaryTime, cancellationToken: cancellationToken);

            var timeStr = summaryTime.ToString(@"hh\:mm");
            await SendMessageAsync(chatId,
                $"Время сводки обновлено! ⏰\n\n" +
                $"Я буду присылать тебе сводку ежедневно в {timeStr}.\n" +
                "Изменить настройки можно через /setsummary.",
                cancellationToken);

            StateManager.ClearState(userId);
            Logger.LogInformation("Время сводки обновлено для пользователя {UserId}: {Time}", userId, timeStr);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка обработки времени сводки для пользователя {UserId}", userId);
            await SendMessageAsync(chatId,
                "Произошла ошибка. Попробуй начать заново с /setsummary",
                cancellationToken);
            StateManager.ClearState(userId);
        }
    }
}
