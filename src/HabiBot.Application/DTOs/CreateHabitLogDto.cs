namespace HabiBot.Application.DTOs;

/// <summary>
/// DTO для создания записи о выполнении привычки
/// </summary>
public record CreateHabitLogDto
{
    /// <summary>
    /// ID привычки
    /// </summary>
    public long HabitId { get; init; }

    /// <summary>
    /// Дата и время выполнения
    /// </summary>
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Заметки от пользователя
    /// </summary>
    public string? Notes { get; init; }
}
