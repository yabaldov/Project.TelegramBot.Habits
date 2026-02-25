using HabiBot.Domain.Enums;
using HabiBot.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace HabiBot.Application.Services;

/// <summary>
/// Сервис для генерации ежедневной сводки по привычкам
/// </summary>
public class DailySummaryService : IDailySummaryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DailySummaryService> _logger;

    public DailySummaryService(IUnitOfWork unitOfWork, ILogger<DailySummaryService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<DailySummaryData> GetDailySummaryAsync(long userId, DateOnly date, TimeOnly? currentUserTime = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Получение сводки для пользователя {UserId} за {Date}", userId, date);

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("Пользователь {UserId} не найден", userId);
            return new DailySummaryData { UserId = userId, Date = date };
        }

        var habits = await _unitOfWork.Habits.GetUserHabitsAsync(userId, cancellationToken);
        var habitsList = habits.ToList();

        var summary = new DailySummaryData
        {
            UserId = userId,
            Date = date
        };

        // Фильтруем привычки, которые запланированы на эту дату
        var plannedHabits = FilterHabitsByDate(habitsList, date);
        summary.PlannedHabitsCount = plannedHabits.Count;

        if (!plannedHabits.Any())
        {
            _logger.LogDebug("У пользователя {UserId} нет привычек, запланированных на {Date}", userId, date);
            return summary;
        }

        var dateStartUtc = date.ToDateTime(TimeOnly.MinValue).ToUniversalTime();
        var dateEndUtc = date.ToDateTime(TimeOnly.MaxValue).ToUniversalTime();

        foreach (var habit in plannedHabits)
        {
            var habitWithLogs = await _unitOfWork.Habits.GetByIdWithLogsAsync(habit.Id, cancellationToken);
            if (habitWithLogs == null) continue;

            // Логи выполнения за этот день
            var logsForDay = habitWithLogs.Logs
                .Where(l => l.CompletedAt >= dateStartUtc && l.CompletedAt < dateEndUtc)
                .OrderBy(l => l.CompletedAt)
                .ToList();

            if (logsForDay.Any())
            {
                // Привычка выполнена
                summary.CompletedHabitsCount++;
                foreach (var log in logsForDay)
                {
                    summary.CompletedHabits.Add(new CompletedHabitInfo
                    {
                        HabitName = habit.Name,
                        CompletedAt = log.CompletedAt
                    });
                }
                summary.AvailableToCompleteToday.Add(new UncompletedHabitInfo
                {
                    HabitId = habit.Id,
                    HabitName = habit.Name,
                    ScheduledTime = habit.ReminderTime
                });
            }
            else
            {
                // Определяем статус привычки по времени напоминания
                var isScheduled = IsHabitStillScheduled(habit.ReminderTime, currentUserTime);

                if (isScheduled)
                {
                    // Время напоминания ещё не наступило — привычка запланирована
                    summary.ScheduledHabits.Add(new UncompletedHabitInfo
                    {
                        HabitId = habit.Id,
                        HabitName = habit.Name,
                        ScheduledTime = habit.ReminderTime
                    });
                }
                else
                {
                    // Время напоминания прошло — привычка не выполнена
                    summary.UncompletedHabits.Add(new UncompletedHabitInfo
                    {
                        HabitId = habit.Id,
                        HabitName = habit.Name,
                        ScheduledTime = habit.ReminderTime
                    });
                }

                // В обоих случаях можно отметить выполнение
                summary.AvailableToCompleteToday.Add(new UncompletedHabitInfo
                {
                    HabitId = habit.Id,
                    HabitName = habit.Name,
                    ScheduledTime = habit.ReminderTime
                });
            }
        }

        // Вычисляем процент выполнения
        summary.CompletionPercentage = summary.PlannedHabitsCount > 0
            ? (summary.CompletedHabitsCount * 100) / summary.PlannedHabitsCount
            : 0;

        // Получаем привычки на следующий день
        var nextDate = date.AddDays(1);
        var nextDayHabits = FilterHabitsByDate(habitsList, nextDate);
        summary.NextDayHabits = nextDayHabits
            .Select(h => new PlannedHabitInfo
            {
                HabitName = h.Name,
                ScheduledTime = h.ReminderTime ?? string.Empty
            })
            .OrderBy(h => h.ScheduledTime)
            .ToList();

        _logger.LogDebug("Сводка для пользователя {UserId} получена: {CompletedCount}/{PlannedCount}", 
            userId, summary.CompletedHabitsCount, summary.PlannedHabitsCount);

        return summary;
    }

    public async Task<string> GenerateSummaryTextAsync(long userId, DateOnly date, bool includeNextDay = true, TimeOnly? currentUserTime = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Генерация текста сводки для пользователя {UserId} за {Date}", userId, date);

        var summary = await GetDailySummaryAsync(userId, date, currentUserTime, cancellationToken);

        var lines = new List<string>();

        // Заголовок
        var dateStr = date.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("ru-RU"));
        lines.Add($"📊 <b>Сводка за {dateStr}</b>\n");

        // Общая статистика
        if (summary.PlannedHabitsCount == 0)
        {
            lines.Add("У вас не было запланировано привычек на этот день.");
            return string.Join("\n", lines);
        }

        lines.Add($"Сегодня ты выполнил <b>{summary.CompletedHabitsCount} из {summary.PlannedHabitsCount}</b> привычек ({summary.CompletionPercentage}%)\n");

        // Выполненные привычки
        if (summary.CompletedHabits.Any())
        {
            lines.Add("✅ <b>Выполненные привычки:</b>");
            foreach (var habit in summary.CompletedHabits.OrderBy(h => h.CompletedAt))
            {
                var timeStr = habit.CompletedAt.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                lines.Add($"  • {habit.HabitName} - {timeStr}");
            }
            lines.Add("");
        }

        // Невыполненные привычки
        if (summary.UncompletedHabits.Any())
        {
            lines.Add("❌ <b>Невыполненные привычки:</b>");
            foreach (var habit in summary.UncompletedHabits.OrderBy(h => h.ScheduledTime))
            {
                var timeStr = !string.IsNullOrEmpty(habit.ScheduledTime) ? habit.ScheduledTime : "без времени";
                lines.Add($"  • {habit.HabitName} - {timeStr}");
            }
            lines.Add("");
        }

        // Запланированные привычки (время напоминания ещё не наступило)
        if (summary.ScheduledHabits.Any())
        {
            lines.Add("🕐 <b>Запланировано:</b>");
            foreach (var habit in summary.ScheduledHabits.OrderBy(h => h.ScheduledTime))
            {
                var timeStr = !string.IsNullOrEmpty(habit.ScheduledTime) ? habit.ScheduledTime : "без времени";
                lines.Add($"  • {habit.HabitName} - {timeStr}");
            }
            lines.Add("");
        }

        // План на следующий день
        if (includeNextDay && summary.NextDayHabits.Any())
        {
            lines.Add("📅 <b>Завтра у тебя запланированы:</b>");
            foreach (var habit in summary.NextDayHabits.OrderBy(h => h.ScheduledTime))
            {
                var timeStr = !string.IsNullOrEmpty(habit.ScheduledTime) ? habit.ScheduledTime : "без времени";
                lines.Add($"  • {habit.HabitName} - {timeStr}");
            }
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Фильтрует привычки по дате на основе их частоты
    /// </summary>
    private List<Domain.Entities.Habit> FilterHabitsByDate(List<Domain.Entities.Habit> habits, DateOnly date)
    {
        return habits
            .Where(h => IsHabitScheduledForDate(h, date))
            .ToList();
    }

    /// <summary>
    /// Проверяет, запланирована ли привычка на указанную дату
    /// </summary>
    private bool IsHabitScheduledForDate(Domain.Entities.Habit habit, DateOnly date)
    {
        // Проверяем, не удалена ли привычка
        if (habit.IsDeleted)
        {
            return false;
        }

        return habit.Frequency switch
        {
            HabitFrequency.Daily => true,
            HabitFrequency.Weekly => IsWeeklyMatch(habit.CreatedAt.Date, date),
            HabitFrequency.Custom => true, // Custom обрабатываем как Daily
            _ => false
        };
    }

    /// <summary>
    /// Проверяет, совпадает ли день недели
    /// </summary>
    private bool IsWeeklyMatch(DateTime createdDate, DateOnly currentDate)
    {
        return createdDate.DayOfWeek == currentDate.ToDateTime(TimeOnly.MinValue).DayOfWeek
               && (currentDate.ToDateTime(TimeOnly.MinValue) - createdDate).Days >= 0;
    }

    /// <summary>
    /// Проверяет, ещё ли не наступило время напоминания для привычки.
    /// Возвращает true, если время напоминания задано и текущее время ≤ времени напоминания.
    /// </summary>
    private static bool IsHabitStillScheduled(string? reminderTime, TimeOnly? currentUserTime)
    {
        if (currentUserTime == null || string.IsNullOrEmpty(reminderTime))
        {
            return false;
        }

        if (TimeOnly.TryParse(reminderTime, out var habitTime))
        {
            return currentUserTime.Value <= habitTime;
        }

        return false;
    }
}
