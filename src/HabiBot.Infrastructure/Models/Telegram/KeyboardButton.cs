using System.Text.Json.Serialization;

namespace HabiBot.Infrastructure.Models.Telegram;

/// <summary>
/// Кнопка Reply клавиатуры
/// </summary>
public record KeyboardButton
{
    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;
}
