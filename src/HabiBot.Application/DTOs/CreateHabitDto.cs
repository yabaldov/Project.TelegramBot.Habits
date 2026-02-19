using HabiBot.Domain.Enums;

namespace HabiBot.Application.DTOs;

/// <summary>
/// DTO для создания привычки
/// </summary>
public record CreateHabitDto
{
    /// <summary>
    /// Название привычки
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// ID пользователя
    /// </summary>
    public long UserId { get; init; }

    /// <summary>
    /// Время напоминания в формате HH:mm
    /// </summary>
    public string? ReminderTime { get; init; }

    /// <summary>
    /// Частота выполнения
    /// </summary>
    public HabitFrequency Frequency { get; init; } = HabitFrequency.Daily;
}
