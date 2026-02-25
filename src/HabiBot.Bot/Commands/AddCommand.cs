using HabiBot.Application.DTOs;
using HabiBot.Application.Services;
using HabiBot.Bot.StateManagement;
using HabiBot.Domain.Enums;
using HabiBot.Infrastructure.Models.Telegram;
using HabiBot.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace HabiBot.Bot.Commands;

/// <summary>
/// Команда /add - добавление новой привычки
/// </summary>
public class AddCommand : BotCommandBase
{
    private readonly IUserService _userService;
    private readonly IHabitService _habitService;

    public AddCommand(
        ITelegramApiClient telegramClient,
        IUserStateManager stateManager,
        IUserService userService,
        IHabitService habitService,
        ILogger<AddCommand> logger)
        : base(telegramClient, stateManager, logger)
    {
        _userService = userService;
        _habitService = habitService;
    }

    public override string Name => "add";
    public override string Description => "Добавить новую привычку";

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

            // Сохраняем ID пользователя в контексте
            StateManager.SetData(userId.Value, "UserId", user.Id);

            // Начинаем процесс добавления привычки
            await SendMessageAsync(chatId.Value,
                "Давай добавим новую привычку! 📝\n\n" +
                "Как она называется?",
                cancellationToken);

            StateManager.SetState(userId.Value, UserState.WaitingForHabitName);
            Logger.LogInformation("Начато добавление привычки для пользователя {UserId}", userId.Value);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка выполнения команды /add для пользователя {UserId}", userId);
            await SendMessageAsync(chatId.Value,
                "Произошла ошибка. Попробуй позже.",
                cancellationToken);
        }
    }

    /// <summary>
    /// Обработка ввода названия привычки
    /// </summary>
    public async Task HandleHabitNameInputAsync(Update update, string habitName, CancellationToken cancellationToken = default)
    {
        var chatId = GetChatId(update);
        var userId = GetUserId(update);

        if (chatId == null || userId == null)
        {
            return;
        }

        try
        {
            // Валидация названия
            if (string.IsNullOrWhiteSpace(habitName) || habitName.Length > 200)
            {
                await SendMessageAsync(chatId.Value,
                    "Название должно содержать от 1 до 200 символов. Попробуй ещё раз:",
                    cancellationToken);
                return;
            }

            // Сохраняем название
            StateManager.SetData(userId.Value, "HabitName", habitName.Trim());

            // Запрашиваем время
            await SendMessageAsync(chatId.Value,
                "Отлично! ✅\n\n" +
                "В какое время тебе напомнить? (Например: 09:00 или 14:30)",
                cancellationToken);

            StateManager.SetState(userId.Value, UserState.WaitingForReminderTime);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка обработки названия привычки для пользователя {UserId}", userId);
            await SendMessageAsync(chatId.Value,
                "Произошла ошибка. Попробуй начать заново с /add",
                cancellationToken);
            StateManager.ClearState(userId.Value);
        }
    }

    /// <summary>
    /// Обработка ввода времени напоминания
    /// </summary>
    public async Task HandleReminderTimeInputAsync(Update update, string timeText, CancellationToken cancellationToken = default)
    {
        var chatId = GetChatId(update);
        var userId = GetUserId(update);

        if (chatId == null || userId == null)
        {
            return;
        }

        try
        {
            // Валидация формата времени (HH:mm)
            var timeRegex = new Regex(@"^([0-1]?[0-9]|2[0-3]):([0-5][0-9])$");
            var match = timeRegex.Match(timeText.Trim());

            if (!match.Success)
            {
                await SendMessageAsync(chatId.Value,
                    "Неверный формат времени. ⚠️\n\n" +
                    "Используй формат ЧЧ:ММ (например: 09:00 или 14:30):",
                    cancellationToken);
                return;
            }

            // Нормализуем формат времени (добавляем ведущий 0 если нужно)
            var hours = match.Groups[1].Value.PadLeft(2, '0');
            var minutes = match.Groups[2].Value;
            var normalizedTime = $"{hours}:{minutes}";

            // Получаем данные из контекста
            var userIdValue = StateManager.GetData<long>(userId.Value, "UserId");
            var habitName = StateManager.GetData<string>(userId.Value, "HabitName");

            if (userIdValue == 0 || string.IsNullOrEmpty(habitName))
            {
                await SendMessageAsync(chatId.Value,
                    "Произошла ошибка. Попробуй начать заново с /add",
                    cancellationToken);
                StateManager.ClearState(userId.Value);
                return;
            }

            // Создаем привычку
            var createHabitDto = new CreateHabitDto
            {
                UserId = userIdValue,
                Name = habitName,
                ReminderTime = normalizedTime,
                Frequency = HabitFrequency.Daily
            };

            await _habitService.CreateHabitAsync(createHabitDto, cancellationToken);

            await SendMessageAsync(chatId.Value,
                $"Привычка \"{habitName}\" добавлена! 🎉\n\n" +
                $"Я буду напоминать тебе каждый день в {normalizedTime}.\n\n" +
                "Используй /list чтобы посмотреть все привычки.",
                cancellationToken);

            // Отправляем сообщение о настройках сводки
            var user = await _userService.GetByTelegramIdAsync(userId.Value, cancellationToken);
            if (user != null)
            {
                var summaryMessage = GetSummaryInfoMessage(user);
                await SendMessageAsync(chatId.Value, summaryMessage, cancellationToken);
            }

            StateManager.ClearState(userId.Value);
            Logger.LogInformation("Привычка {HabitName} создана для пользователя {UserId}", habitName, userId.Value);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка создания привычки для пользователя {UserId}", userId);
            await SendMessageAsync(chatId.Value,
                "Произошла ошибка при создании привычки. Попробуй начать заново с /add",
                cancellationToken);
            StateManager.ClearState(userId.Value);
        }
    }

    /// <summary>
    /// Генерирует информационное сообщение о настройках ежедневной сводки
    /// </summary>
    private static string GetSummaryInfoMessage(Domain.Entities.User user)
    {
        if (!user.IsDailySummaryEnabled)
        {
            return "ℹ️ Сводка о выполнении привычек отключена. Ты всегда можешь включить её через /setsummary.";
        }

        var timeStr = user.DailySummaryTime?.ToString(@"hh\:mm") ?? "21:00";
        return $"ℹ️ Я буду присылать тебе сводку о выполнении привычек ежедневно в {timeStr}. Это поведение можно изменить или отключить через /setsummary.";
    }
}
