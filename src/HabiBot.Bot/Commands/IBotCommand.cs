using HabiBot.Infrastructure.Models.Telegram;

namespace HabiBot.Bot.Commands;

/// <summary>
/// Базовый интерфейс для команд бота
/// </summary>
public interface IBotCommand
{
    /// <summary>
    /// Название команды (например, "start", "add")
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Описание команды
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Выполнить команду
    /// </summary>
    Task ExecuteAsync(Update update, CancellationToken cancellationToken = default);
}
