using System.Text.Json.Serialization;

namespace HabiBot.Infrastructure.Models.Telegram;

/// <summary>
/// Запрос на получение обновлений (long polling)
/// </summary>
public record GetUpdatesRequest
{
    [JsonPropertyName("offset")]
    public long? Offset { get; init; }

    [JsonPropertyName("limit")]
    public int? Limit { get; init; }

    [JsonPropertyName("timeout")]
    public int? Timeout { get; init; }

    [JsonPropertyName("allowed_updates")]
    public string[]? AllowedUpdates { get; init; }
}
