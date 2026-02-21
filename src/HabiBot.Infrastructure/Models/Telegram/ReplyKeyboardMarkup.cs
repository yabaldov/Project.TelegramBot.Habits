using System.Text.Json.Serialization;

namespace HabiBot.Infrastructure.Models.Telegram;

/// <summary>
/// Reply клавиатура (кнопки под полем ввода)
/// </summary>
public record ReplyKeyboardMarkup
{
    [JsonPropertyName("keyboard")]
    public IEnumerable<IEnumerable<KeyboardButton>> Keyboard { get; init; } = Array.Empty<IEnumerable<KeyboardButton>>();

    [JsonPropertyName("resize_keyboard")]
    public bool ResizeKeyboard { get; init; } = true;

    [JsonPropertyName("one_time_keyboard")]
    public bool OneTimeKeyboard { get; init; }
}
