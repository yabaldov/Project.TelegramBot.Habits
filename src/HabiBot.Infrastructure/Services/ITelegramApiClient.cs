using HabiBot.Infrastructure.Models.Telegram;

namespace HabiBot.Infrastructure.Services;

/// <summary>
/// Интерфейс клиента для работы с Telegram Bot API
/// </summary>
public interface ITelegramApiClient
{
    /// <summary>
    /// Отправить текстовое сообщение
    /// </summary>
    Task<Message> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить обновления (long polling)
    /// </summary>
    Task<Update[]> GetUpdatesAsync(GetUpdatesRequest? request = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить информацию о боте
    /// </summary>
    Task<TelegramUser> GetMeAsync(CancellationToken cancellationToken = default);
}
