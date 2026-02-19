using HabiBot.Domain.Entities;

namespace HabiBot.Domain.Interfaces;

/// <summary>
/// Репозиторий для записей выполнения привычек
/// </summary>
public interface IHabitLogRepository : IRepository<HabitLog>
{
    /// <summary>
    /// Получить записи выполнения конкретной привычки
    /// </summary>
    Task<IEnumerable<HabitLog>> GetHabitLogsAsync(long habitId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить записи выполнения привычки за период
    /// </summary>
    Task<IEnumerable<HabitLog>> GetHabitLogsByDateRangeAsync(
        long habitId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверить, была ли выполнена привычка в конкретную дату
    /// </summary>
    Task<bool> IsCompletedOnDateAsync(
        long habitId,
        DateTime date,
        CancellationToken cancellationToken = default);
}
