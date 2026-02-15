namespace HabiBot.Domain.Entities;

/// <summary>
/// Сущность пользователя Telegram
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// Имя пользователя
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Идентификатор чата Telegram (равен Id для пользователей)
    /// </summary>
    public long TelegramChatId { get; set; }

    /// <summary>
    /// Username пользователя в Telegram (без @)
    /// </summary>
    public string? TelegramUserName { get; set; }

    /// <summary>
    /// Дата и время регистрации в боте
    /// </summary>
    public DateTime RegisteredAt { get; set; }

    /// <summary>
    /// Часовой пояс пользователя (например, "Europe/Moscow")
    /// </summary>
    public string? TimeZone { get; set; }

    /// <summary>
    /// Коллекция привычек пользователя
    /// </summary>
    public ICollection<Habit> Habits { get; set; } = new List<Habit>();
}
