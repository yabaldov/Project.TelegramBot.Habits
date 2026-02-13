using HabiBot.Domain.Entities;

namespace HabiBot.Domain.Interfaces;

/// <summary>
/// Репозиторий для сущности пользователя
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Получить пользователя по Telegram ID
    /// </summary>
    Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверить существование пользователя по Telegram ID
    /// </summary>
    Task<bool> ExistsByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default);
}
