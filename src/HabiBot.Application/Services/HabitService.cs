using FluentValidation;
using HabiBot.Application.DTOs;
using HabiBot.Domain.Entities;
using HabiBot.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace HabiBot.Application.Services;

/// <summary>
/// Реализация сервиса для работы с привычками
/// </summary>
public class HabitService : IHabitService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateHabitDto> _createValidator;
    private readonly IValidator<UpdateHabitDto> _updateValidator;
    private readonly ILogger<HabitService> _logger;

    public HabitService(
        IUnitOfWork unitOfWork,
        IValidator<CreateHabitDto> createValidator,
        IValidator<UpdateHabitDto> updateValidator,
        ILogger<HabitService> logger)
    {
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    public async Task<Habit> CreateHabitAsync(CreateHabitDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Создание привычки для пользователя {UserId}: {Name}", dto.UserId, dto.Name);

        // Валидация
        await _createValidator.ValidateAndThrowAsync(dto, cancellationToken);

        // Проверка существования пользователя
        var userExists = await _unitOfWork.Users.ExistsAsync(dto.UserId, cancellationToken);
        if (!userExists)
        {
            _logger.LogWarning("Пользователь {UserId} не найден", dto.UserId);
            throw new InvalidOperationException($"Пользователь с ID {dto.UserId} не найден");
        }

        // Создание привычки
        var habit = new Habit
        {
            Name = dto.Name,
            UserId = dto.UserId,
            ReminderTime = dto.ReminderTime,
            Frequency = dto.Frequency
        };

        await _unitOfWork.Habits.AddAsync(habit, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Привычка создана: {HabitId}, {Name}", habit.Id, habit.Name);

        return habit;
    }

    public async Task<Habit> UpdateHabitAsync(UpdateHabitDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Обновление привычки {HabitId}", dto.Id);

        // Валидация
        await _updateValidator.ValidateAndThrowAsync(dto, cancellationToken);

        // Получение привычки
        var habit = await _unitOfWork.Habits.GetByIdAsync(dto.Id, cancellationToken);
        if (habit == null)
        {
            _logger.LogWarning("Привычка {HabitId} не найдена", dto.Id);
            throw new InvalidOperationException($"Привычка с ID {dto.Id} не найдена");
        }

        // Обновление полей
        if (!string.IsNullOrEmpty(dto.Name))
        {
            habit.Name = dto.Name;
        }

        if (dto.ReminderTime != null)
        {
            habit.ReminderTime = dto.ReminderTime;
        }

        if (dto.Frequency.HasValue)
        {
            habit.Frequency = dto.Frequency.Value;
        }

        await _unitOfWork.Habits.UpdateAsync(habit, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Привычка обновлена: {HabitId}", habit.Id);

        return habit;
    }

    public async Task DeleteHabitAsync(long habitId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Удаление привычки {HabitId}", habitId);

        var habit = await _unitOfWork.Habits.GetByIdAsync(habitId, cancellationToken);
        if (habit == null)
        {
            _logger.LogWarning("Привычка {HabitId} не найдена", habitId);
            throw new InvalidOperationException($"Привычка с ID {habitId} не найдена");
        }

        await _unitOfWork.Habits.DeleteAsync(habit, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Привычка удалена: {HabitId}", habitId);
    }

    public async Task<Habit?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Поиск привычки по ID: {HabitId}", id);
        return await _unitOfWork.Habits.GetByIdAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<Habit>> GetUserHabitsAsync(long userId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Получение привычек пользователя {UserId}", userId);
        return await _unitOfWork.Habits.GetUserHabitsAsync(userId, cancellationToken);
    }

    public async Task<Habit?> GetByIdWithLogsAsync(long id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Поиск привычки с логами по ID: {HabitId}", id);
        return await _unitOfWork.Habits.GetByIdWithLogsAsync(id, cancellationToken);
    }
}
