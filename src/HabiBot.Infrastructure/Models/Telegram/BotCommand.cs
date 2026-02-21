using System.Text.Json.Serialization;

namespace HabiBot.Infrastructure.Models.Telegram;

/// <summary>
/// Команда бота для отображения в меню Telegram
/// </summary>
public class BotCommand
{
    /// <summary>
    /// Название команды (без слэша, например "start")
    /// </summary>
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Описание команды
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Запрос на установку списка команд бота
/// </summary>
public class SetMyCommandsRequest
{
    /// <summary>
    /// Список команд бота
    /// </summary>
    [JsonPropertyName("commands")]
    public List<BotCommand> Commands { get; set; } = new();
}
