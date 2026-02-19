using HabiBot.Application.DTOs;
using HabiBot.Domain.Entities;

namespace HabiBot.Application.Services;

/// <summary>
/// Интерфейс сервиса для работы с привычками
/// </summary>
public interface IHabitService
{
    /// <summary>
    /// Создать новую привычку
    /// </summary>
    Task<Habit> CreateHabitAsync(CreateHabitDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновить привычку
    /// </summary>
    Task<Habit> UpdateHabitAsync(UpdateHabitDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удалить привычку
    /// </summary>
    Task DeleteHabitAsync(long habitId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить привычку по ID
    /// </summary>
    Task<Habit?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить все привычки пользователя
    /// </summary>
    Task<IEnumerable<Habit>> GetUserHabitsAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить привычку с записями выполнения
    /// </summary>
    Task<Habit?> GetByIdWithLogsAsync(long id, CancellationToken cancellationToken = default);
}
