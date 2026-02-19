using FluentValidation;
using HabiBot.Application.DTOs;
using HabiBot.Domain.Entities;
using HabiBot.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace HabiBot.Application.Services;

/// <summary>
/// Реализация сервиса для работы с пользователями
/// </summary>
public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateUserDto> _createValidator;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUnitOfWork unitOfWork,
        IValidator<CreateUserDto> createValidator,
        ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _logger = logger;
    }

    public async Task<User> CreateUserAsync(CreateUserDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Создание пользователя с Telegram ID: {TelegramId}", dto.TelegramChatId);

        // Валидация
        await _createValidator.ValidateAndThrowAsync(dto, cancellationToken);

        // Проверка существования
        var exists = await _unitOfWork.Users.ExistsByTelegramIdAsync(dto.TelegramUserId, cancellationToken);
        if (exists)
        {
            _logger.LogWarning("Пользователь с Telegram ID {TelegramId} уже существует", dto.TelegramUserId);
            throw new InvalidOperationException($"Пользователь с Telegram ID {dto.TelegramUserId} уже существует");
        }

        // Создание пользователя
        var user = new User
        {
            Name = dto.Name,
            TelegramUserId = dto.TelegramUserId,
            TelegramChatId = dto.TelegramChatId,
            TelegramFirstName = dto.TelegramFirstName,
            TelegramLastName = dto.TelegramLastName,
            TelegramUserName = dto.TelegramUserName,
            RegisteredAt = DateTime.UtcNow,
            TimeZone = dto.TimeZone
        };

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Пользователь создан: {UserId}, Telegram ID: {TelegramId}", user.Id, user.TelegramUserId);

        return user;
    }

    public async Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Поиск пользователя по Telegram ID: {TelegramId}", telegramId);
        return await _unitOfWork.Users.GetByTelegramIdAsync(telegramId, cancellationToken);
    }

    public async Task<bool> ExistsByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Users.ExistsByTelegramIdAsync(telegramId, cancellationToken);
    }

    public async Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Поиск пользователя по ID: {UserId}", id);
        return await _unitOfWork.Users.GetByIdAsync(id, cancellationToken);
    }

    public async Task UpdateDailySummarySettingsAsync(long userId, bool isEnabled, TimeSpan? summaryTime = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Обновление настроек сводки для пользователя {UserId}: Enabled={IsEnabled}, Time={SummaryTime}",
            userId, isEnabled, summaryTime);

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"Пользователь с ID {userId} не найден");

        user.IsDailySummaryEnabled = isEnabled;

        if (summaryTime.HasValue)
        {
            user.DailySummaryTime = summaryTime.Value;
        }

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Настройки сводки обновлены для пользователя {UserId}", userId);
    }
}
