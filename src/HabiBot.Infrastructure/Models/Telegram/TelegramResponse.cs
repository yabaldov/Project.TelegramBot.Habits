using System.Text.Json.Serialization;

namespace HabiBot.Infrastructure.Models.Telegram;

/// <summary>
/// Базовый ответ Telegram API
/// </summary>
/// <typeparam name="T">Тип результата</typeparam>
public record TelegramResponse<T>
{
    [JsonPropertyName("ok")]
    public bool Ok { get; init; }

    [JsonPropertyName("result")]
    public T? Result { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("error_code")]
    public int? ErrorCode { get; init; }
}
