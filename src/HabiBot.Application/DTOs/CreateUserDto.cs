namespace HabiBot.Application.DTOs;

/// <summary>
/// DTO для создания пользователя
/// </summary>
public record CreateUserDto
{
    /// <summary>
    /// Отображаемое имя (кастомное)
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Telegram User ID
    /// </summary>
    public long TelegramUserId { get; init; }

    /// <summary>
    /// Telegram Chat ID (для private чатов равен TelegramUserId)
    /// </summary>
    public long TelegramChatId { get; init; }

    /// <summary>
    /// Имя в Telegram (First Name)
    /// </summary>
    public string? TelegramFirstName { get; init; }

    /// <summary>
    /// Фамилия в Telegram (Last Name)
    /// </summary>
    public string? TelegramLastName { get; init; }

    /// <summary>
    /// Username в Telegram (без @)
    /// </summary>
    public string? TelegramUserName { get; init; }

    /// <summary>
    /// Часовой пояс пользователя
    /// </summary>
    public string? TimeZone { get; init; }
}
