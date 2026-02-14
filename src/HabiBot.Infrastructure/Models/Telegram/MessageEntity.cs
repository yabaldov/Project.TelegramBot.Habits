using System.Text.Json.Serialization;

namespace HabiBot.Infrastructure.Models.Telegram;

/// <summary>
/// Message Entity (для команд, хэштегов и т.д.)
/// </summary>
public record MessageEntity
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("offset")]
    public int Offset { get; init; }

    [JsonPropertyName("length")]
    public int Length { get; init; }
}
