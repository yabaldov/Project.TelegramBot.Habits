namespace HabiBot.Domain.Entities;

/// <summary>
/// Сущность пользователя Telegram
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// Отображаемое имя пользователя (кастомное, устанавливается при регистрации)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Уникальный идентификатор пользователя в Telegram
    /// </summary>
    public long TelegramUserId { get; set; }

    /// <summary>
    /// Идентификатор чата с пользователем (для private чатов равен TelegramUserId)
    /// </summary>
    public long TelegramChatId { get; set; }

    /// <summary>
    /// Имя пользователя в Telegram (First Name)
    /// </summary>
    public string? TelegramFirstName { get; set; }

    /// <summary>
    /// Фамилия пользователя в Telegram (Last Name)
    /// </summary>
    public string? TelegramLastName { get; set; }

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
