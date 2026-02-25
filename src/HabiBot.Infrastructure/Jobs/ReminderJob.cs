using HabiBot.Application.Services;
using HabiBot.Domain.Interfaces;
using HabiBot.Infrastructure.Models.Telegram;
using HabiBot.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace HabiBot.Infrastructure.Jobs;

/// <summary>
/// Quartz Job для отправки напоминаний о привычках.
/// Запускается каждую минуту и проверяет, каким пользователям нужно отправить напоминание.
/// </summary>
[DisallowConcurrentExecution]
public class ReminderJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReminderJob> _logger;

    public ReminderJob(
        IServiceScopeFactory scopeFactory,
        ILogger<ReminderJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogDebug("Запуск ReminderJob");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var telegramClient = scope.ServiceProvider.GetRequiredService<ITelegramApiClient>();

            // Получаем все активные привычки с установленным временем напоминания
            var habits = await unitOfWork.Habits.GetHabitsWithReminderAsync(context.CancellationToken);
            var habitsList = habits.ToList();

            if (!habitsList.Any())
            {
                _logger.LogDebug("Нет привычек с настроенными напоминаниями");
                return;
            }

            var now = DateTime.UtcNow;
            var sentCount = 0;

            foreach (var habit in habitsList)
            {
                try
                {
                    if (!ShouldSendReminderNow(habit, now))
                    {
                        continue;
                    }

                    _logger.LogInformation(
                        "Отправка напоминания о привычке {HabitName} (Id={HabitId}) пользователю {TelegramId}",
                        habit.Name, habit.Id, habit.User.TelegramUserId);

                    var keyboard = BuildReminderKeyboard(habit.Id);

                    var request = new SendMessageRequest
                    {
                        ChatId = habit.User.TelegramChatId,
                        Text = BuildReminderText(habit.Name),
                        ParseMode = "HTML",
                        ReplyMarkup = keyboard
                    };

                    await telegramClient.SendMessageAsync(request, context.CancellationToken);

                    sentCount++;
                    _logger.LogInformation(
                        "Напоминание отправлено пользователю {TelegramId} о привычке '{HabitName}'",
                        habit.User.TelegramUserId, habit.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Ошибка отправки напоминания о привычке {HabitId} пользователю {TelegramId}",
                        habit.Id, habit.User.TelegramUserId);
                }
            }

            _logger.LogInformation("ReminderJob завершён. Отправлено напоминаний: {Count}", sentCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Критическая ошибка в ReminderJob");
        }
    }

    /// <summary>
    /// Проверяет, нужно ли отправлять напоминание о привычке прямо сейчас.
    /// Job запускается каждую минуту, поэтому проверяем совпадение часа и минуты
    /// с учётом временной зоны пользователя.
    /// </summary>
    private bool ShouldSendReminderNow(Domain.Entities.Habit habit, DateTime utcNow)
    {
        if (string.IsNullOrEmpty(habit.ReminderTime))
        {
            return false;
        }

        // Парсим строку формата "HH:mm"
        if (!TryParseReminderTime(habit.ReminderTime, out var reminderHour, out var reminderMinute))
        {
            _logger.LogWarning("Не удалось разобрать ReminderTime '{ReminderTime}' для привычки {HabitId}",
                habit.ReminderTime, habit.Id);
            return false;
        }

        var userNow = GetUserDateTime(habit.User, utcNow);

        return userNow.Hour == reminderHour && userNow.Minute == reminderMinute;
    }

    /// <summary>
    /// Парсит время в формате "HH:mm"
    /// </summary>
    private static bool TryParseReminderTime(string timeStr, out int hour, out int minute)
    {
        hour = 0;
        minute = 0;

        var parts = timeStr.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        return int.TryParse(parts[0], out hour) && int.TryParse(parts[1], out minute);
    }

    /// <summary>
    /// Возвращает текущее время пользователя с учётом его временной зоны
    /// </summary>
    private static DateTime GetUserDateTime(Domain.Entities.User user, DateTime utcNow)
    {
        var tzInfo = TimeZoneParser.ToTimeZoneInfo(user.TimeZone);
        if (tzInfo == null)
        {
            return utcNow;
        }

        return TimeZoneInfo.ConvertTimeFromUtc(utcNow, tzInfo);
    }

    /// <summary>
    /// Формирует текст напоминания для привычки
    /// </summary>
    private static string BuildReminderText(string habitName)
    {
        return $"⏰ <b>Напоминание о привычке</b>\n\n" +
               $"Пришло время выполнить: <b>{habitName}</b>\n\n" +
               "Нажми кнопку ниже, когда выполнишь, или отметь через /list.";
    }

    /// <summary>
    /// Строит inline клавиатуру с кнопкой «Выполнено» для быстрой отметки
    /// </summary>
    private static InlineKeyboardMarkup BuildReminderKeyboard(long habitId)
    {
        return new InlineKeyboardMarkup
        {
            InlineKeyboard = new List<List<InlineKeyboardButton>>
            {
                new()
                {
                    new InlineKeyboardButton
                    {
                        Text = "✅ Выполнено!",
                        CallbackData = $"summarycomplete:{habitId}"
                    }
                }
            }
        };
    }
}
