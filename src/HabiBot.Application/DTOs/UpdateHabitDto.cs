using HabiBot.Domain.Enums;

namespace HabiBot.Application.DTOs;

/// <summary>
/// DTO для обновления привычки
/// </summary>
public record UpdateHabitDto
{
    /// <summary>
    /// ID привычки
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// Новое название привычки
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Новое время напоминания
    /// </summary>
    public string? ReminderTime { get; init; }

    /// <summary>
    /// Новая частота выполнения
    /// </summary>
    public HabitFrequency? Frequency { get; init; }
}
