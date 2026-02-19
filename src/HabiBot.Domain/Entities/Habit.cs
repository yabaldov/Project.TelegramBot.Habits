using HabiBot.Domain.Enums;

namespace HabiBot.Domain.Entities;

/// <summary>
/// Сущность привычки
/// </summary>
public class Habit : BaseEntity
{
    /// <summary>
    /// Название привычки
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Идентификатор пользователя-владельца привычки
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Навигационное свойство к пользователю
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Время напоминания в формате HH:mm (например, "08:00")
    /// </summary>
    public string? ReminderTime { get; set; }

    /// <summary>
    /// Частота выполнения привычки
    /// </summary>
    public HabitFrequency Frequency { get; set; } = HabitFrequency.Daily;

    /// <summary>
    /// Коллекция записей о выполнении привычки
    /// </summary>
    public ICollection<HabitLog> Logs { get; set; } = new List<HabitLog>();
}
