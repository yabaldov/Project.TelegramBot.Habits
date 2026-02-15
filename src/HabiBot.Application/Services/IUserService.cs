using HabiBot.Application.DTOs;
using HabiBot.Domain.Entities;

namespace HabiBot.Application.Services;

/// <summary>
/// Интерфейс сервиса для работы с пользователями
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Создать нового пользователя
    /// </summary>
    Task<User> CreateUserAsync(CreateUserDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить пользователя по Telegram ID
    /// </summary>
    Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверить существование пользователя по Telegram ID
    /// </summary>
    Task<bool> ExistsByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить пользователя по ID
    /// </summary>
    Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}
