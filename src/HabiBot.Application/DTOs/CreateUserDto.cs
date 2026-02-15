namespace HabiBot.Application.DTOs;

/// <summary>
/// DTO для создания пользователя
/// </summary>
public record CreateUserDto
{
    /// <summary>
    /// Имя пользователя
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Telegram ID пользователя
    /// </summary>
    public long TelegramChatId { get; init; }

    /// <summary>
    /// Username в Telegram (без @)
    /// </summary>
    public string? TelegramUserName { get; init; }

    /// <summary>
    /// Часовой пояс пользователя
    /// </summary>
    public string? TimeZone { get; init; }
}
