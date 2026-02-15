using System.Text.Json.Serialization;

namespace HabiBot.Infrastructure.Models.Telegram;

/// <summary>
/// Telegram Update DTO
/// </summary>
public record Update
{
    [JsonPropertyName("update_id")]
    public long UpdateId { get; init; }

    [JsonPropertyName("message")]
    public Message? Message { get; init; }

    [JsonPropertyName("edited_message")]
    public Message? EditedMessage { get; init; }

    [JsonPropertyName("channel_post")]
    public Message? ChannelPost { get; init; }

    [JsonPropertyName("edited_channel_post")]
    public Message? EditedChannelPost { get; init; }

    [JsonPropertyName("callback_query")]
    public CallbackQuery? CallbackQuery { get; init; }
}
