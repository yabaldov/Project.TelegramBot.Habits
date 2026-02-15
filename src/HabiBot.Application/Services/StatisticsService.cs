using HabiBot.Application.DTOs;
using HabiBot.Domain.Enums;
using HabiBot.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace HabiBot.Application.Services;

/// <summary>
/// Сервис для вычисления статистики привычек
/// </summary>
public class StatisticsService : IStatisticsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StatisticsService> _logger;

    public StatisticsService(IUnitOfWork unitOfWork, ILogger<StatisticsService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UserStatistics> GetDailyStatisticsAsync(long userId, DateTime date, CancellationToken cancellationToken = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await GetStatisticsForPeriodAsync(userId, startOfDay, endOfDay, cancellationToken);
    }

    public async Task<UserStatistics> GetWeeklyStatisticsAsync(long userId, DateTime startOfWeek, CancellationToken cancellationToken = default)
    {
        var endOfWeek = startOfWeek.AddDays(7);

        return await GetStatisticsForPeriodAsync(userId, startOfWeek, endOfWeek, cancellationToken);
    }

    public async Task<UserStatistics> GetMonthlyStatisticsAsync(long userId, int year, int month, CancellationToken cancellationToken = default)
    {
        var startOfMonth = new DateTime(year, month, 1);
        var endOfMonth = startOfMonth.AddMonths(1);

        return await GetStatisticsForPeriodAsync(userId, startOfMonth, endOfMonth, cancellationToken);
    }

    public async Task<UserStatistics> GetStatisticsForPeriodAsync(long userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Получение статистики для пользователя {UserId} с {StartDate} по {EndDate}", userId, startDate, endDate);

        // Получаем привычки пользователя
        var habits = await _unitOfWork.Habits.GetUserHabitsAsync(userId, cancellationToken);
        var habitsList = habits.ToList();

        if (!habitsList.Any())
        {
            _logger.LogDebug("У пользователя {UserId} нет привычек", userId);
            return new UserStatistics
            {
                TotalHabits = 0,
                TotalCompletions = 0,
                CompletionsToday = 0,
                PeriodStart = startDate,
                PeriodEnd = endDate
            };
        }

        var habitStats = new List<HabitStatistics>();
        var totalCompletions = 0;
        var completionsToday = 0;
        var today = DateTime.UtcNow.Date;

        foreach (var habit in habitsList)
        {
            // Получаем привычку с логами
            var habitWithLogs = await _unitOfWork.Habits.GetByIdWithLogsAsync(habit.Id, cancellationToken);
            if (habitWithLogs == null) continue;

            // Фильтруем логи по периоду
            var logsInPeriod = habitWithLogs.Logs
                .Where(l => l.CompletedAt >= startDate && l.CompletedAt < endDate)
                .OrderBy(l => l.CompletedAt)
                .ToList();

            var completedCount = logsInPeriod.Count;
            totalCompletions += completedCount;

            // Подсчитываем выполнения сегодня
            var todayLogs = habitWithLogs.Logs.Count(l => l.CompletedAt.Date == today);
            completionsToday += todayLogs;

            // Вычисляем ожидаемое количество
            var expectedCount = CalculateExpectedCount(habit.Frequency, startDate, endDate);

            // Последнее выполнение
            var lastCompleted = logsInPeriod.Any() ? logsInPeriod.Last().CompletedAt : (DateTime?)null;

            // Вычисляем серии
            var currentStreak = await GetCurrentStreakAsync(habit.Id, cancellationToken);
            var bestStreak = await GetBestStreakAsync(habit.Id, cancellationToken);

            habitStats.Add(new HabitStatistics
            {
                HabitId = habit.Id,
                HabitName = habit.Name,
                CompletedCount = completedCount,
                ExpectedCount = expectedCount,
                LastCompleted = lastCompleted,
                CurrentStreak = currentStreak,
                BestStreak = bestStreak
            });
        }

        return new UserStatistics
        {
            TotalHabits = habitsList.Count,
            TotalCompletions = totalCompletions,
            CompletionsToday = completionsToday,
            HabitStats = habitStats,
            PeriodStart = startDate,
            PeriodEnd = endDate
        };
    }

    public async Task<int> GetCurrentStreakAsync(long habitId, CancellationToken cancellationToken = default)
    {
        var habit = await _unitOfWork.Habits.GetByIdWithLogsAsync(habitId, cancellationToken);
        if (habit == null || !habit.Logs.Any())
            return 0;

        var logs = habit.Logs.OrderByDescending(l => l.CompletedAt).ToList();
        var today = DateTime.UtcNow.Date;
        var streak = 0;
        var checkDate = today;

        // Проверяем выполнение за сегодня или вчера
        if (logs.First().CompletedAt.Date < today.AddDays(-1))
            return 0; // Серия прервана

        foreach (var log in logs)
        {
            var logDate = log.CompletedAt.Date;

            if (logDate == checkDate)
            {
                streak++;
                checkDate = checkDate.AddDays(-1);
            }
            else if (logDate < checkDate.AddDays(-1))
            {
                break; // Пропущен день
            }
        }

        return streak;
    }

    public async Task<int> GetBestStreakAsync(long habitId, CancellationToken cancellationToken = default)
    {
        var habit = await _unitOfWork.Habits.GetByIdWithLogsAsync(habitId, cancellationToken);
        if (habit == null || !habit.Logs.Any())
            return 0;

        var logs = habit.Logs.OrderBy(l => l.CompletedAt).ToList();
        var maxStreak = 0;
        var currentStreak = 0;
        DateTime? lastDate = null;

        foreach (var log in logs)
        {
            var logDate = log.CompletedAt.Date;

            if (lastDate == null || logDate == lastDate.Value.AddDays(1))
            {
                currentStreak++;
                maxStreak = Math.Max(maxStreak, currentStreak);
            }
            else if (logDate > lastDate.Value.AddDays(1))
            {
                currentStreak = 1;
            }

            lastDate = logDate;
        }

        return maxStreak;
    }

    private int CalculateExpectedCount(HabitFrequency frequency, DateTime startDate, DateTime endDate)
    {
        var days = (endDate - startDate).Days;

        return frequency switch
        {
            HabitFrequency.Daily => days,
            HabitFrequency.Weekly => days / 7,
            _ => days // Custom - по умолчанию как Daily
        };
    }
}
