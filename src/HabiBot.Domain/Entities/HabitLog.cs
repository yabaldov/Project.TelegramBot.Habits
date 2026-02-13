namespace HabiBot.Domain.Entities;

/// <summary>
/// Запись о выполнении привычки
/// </summary>
public class HabitLog : BaseEntity
{
    /// <summary>
    /// Идентификатор привычки
    /// </summary>
    public long HabitId { get; set; }

    /// <summary>
    /// Навигационное свойство к привычке
    /// </summary>
    public Habit Habit { get; set; } = null!;

    /// <summary>
    /// Дата и время выполнения привычки (UTC)
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Необязательные заметки от пользователя
    /// </summary>
    public string? Notes { get; set; }
}
