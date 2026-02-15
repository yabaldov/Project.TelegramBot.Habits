using System.Text.Json.Serialization;

namespace HabiBot.Infrastructure.Models.Telegram;

/// <summary>
/// Callback query от inline кнопки
/// </summary>
public record CallbackQuery
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("from")]
    public TelegramUser From { get; init; } = new();

    [JsonPropertyName("message")]
    public Message? Message { get; init; }

    [JsonPropertyName("data")]
    public string? Data { get; init; }
}
