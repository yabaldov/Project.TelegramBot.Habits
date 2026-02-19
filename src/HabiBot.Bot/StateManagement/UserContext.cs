namespace HabiBot.Bot.StateManagement;

/// <summary>
/// Контекст состояния пользователя
/// </summary>
public class UserContext
{
    /// <summary>
    /// Telegram ID пользователя
    /// </summary>
    public long TelegramId { get; set; }

    /// <summary>
    /// Текущее состояние
    /// </summary>
    public UserState State { get; set; } = UserState.None;

    /// <summary>
    /// Временные данные для многошаговых диалогов
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Время последнего обновления
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
