using System.Text.Json.Serialization;

namespace HabiBot.Infrastructure.Models.Telegram;

/// <summary>
/// Запрос на отправку сообщения
/// </summary>
public record SendMessageRequest
{
    [JsonPropertyName("chat_id")]
    public long ChatId { get; init; }

    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;

    [JsonPropertyName("parse_mode")]
    public string? ParseMode { get; init; }

    [JsonPropertyName("reply_to_message_id")]
    public long? ReplyToMessageId { get; init; }

    [JsonPropertyName("reply_markup")]
    public InlineKeyboardMarkup? ReplyMarkup { get; init; }
}
