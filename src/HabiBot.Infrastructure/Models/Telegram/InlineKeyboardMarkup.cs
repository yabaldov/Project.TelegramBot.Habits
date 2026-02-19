using System.Text.Json.Serialization;

namespace HabiBot.Infrastructure.Models.Telegram;

/// <summary>
/// Inline клавиатура
/// </summary>
public record InlineKeyboardMarkup
{
    [JsonPropertyName("inline_keyboard")]
    public IEnumerable<IEnumerable<InlineKeyboardButton>> InlineKeyboard { get; init; } = Array.Empty<IEnumerable<InlineKeyboardButton>>();
}
