using HabiBot.Application.DTOs;

namespace HabiBot.Application.Services;

/// <summary>
/// Интерфейс сервиса статистики
/// </summary>
public interface IStatisticsService
{
    /// <summary>
    /// Получить статистику пользователя за день
    /// </summary>
    Task<UserStatistics> GetDailyStatisticsAsync(long userId, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить статистику пользователя за неделю
    /// </summary>
    Task<UserStatistics> GetWeeklyStatisticsAsync(long userId, DateTime startOfWeek, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить статистику пользователя за месяц
    /// </summary>
    Task<UserStatistics> GetMonthlyStatisticsAsync(long userId, int year, int month, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить статистику пользователя за произвольный период
    /// </summary>
    Task<UserStatistics> GetStatisticsForPeriodAsync(long userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить текущую серию (streak) для привычки
    /// </summary>
    Task<int> GetCurrentStreakAsync(long habitId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить лучшую серию (streak) для привычки
    /// </summary>
    Task<int> GetBestStreakAsync(long habitId, CancellationToken cancellationToken = default);
}
