using HabiBot.Domain.Entities;

namespace HabiBot.Domain.Interfaces;

/// <summary>
/// Репозиторий для сущности привычки
/// </summary>
public interface IHabitRepository : IRepository<Habit>
{
    /// <summary>
    /// Получить все привычки конкретного пользователя
    /// </summary>
    Task<IEnumerable<Habit>> GetUserHabitsAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить привычку по ID с включёнными записями выполнения
    /// </summary>
    Task<Habit?> GetByIdWithLogsAsync(long id, CancellationToken cancellationToken = default);
}
