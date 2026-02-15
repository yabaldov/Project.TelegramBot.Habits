using System.Text.Json.Serialization;

namespace HabiBot.Infrastructure.Models.Telegram;

/// <summary>
/// Кнопка inline клавиатуры
/// </summary>
public record InlineKeyboardButton
{
    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;

    [JsonPropertyName("callback_data")]
    public string? CallbackData { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }
}
