namespace HabiBot.Domain.Entities;

/// <summary>
/// Базовая сущность с полями аудита
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Идентификатор сущности
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Дата и время создания (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время последнего обновления (UTC)
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Флаг мягкого удаления
    /// </summary>
    public bool IsDeleted { get; set; }
}
