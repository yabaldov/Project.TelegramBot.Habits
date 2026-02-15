using HabiBot.Application.DTOs;
using HabiBot.Domain.Entities;

namespace HabiBot.Application.Services;

/// <summary>
/// Интерфейс сервиса для работы с записями выполнения привычек
/// </summary>
public interface IHabitLogService
{
    /// <summary>
    /// Создать запись о выполнении привычки
    /// </summary>
    Task<HabitLog> CreateLogAsync(CreateHabitLogDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить записи выполнения привычки
    /// </summary>
    Task<IEnumerable<HabitLog>> GetHabitLogsAsync(long habitId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить записи выполнения за период
    /// </summary>
    Task<IEnumerable<HabitLog>> GetHabitLogsByDateRangeAsync(
        long habitId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверить, выполнена ли привычка сегодня
    /// </summary>
    Task<bool> IsCompletedTodayAsync(long habitId, CancellationToken cancellationToken = default);
}
