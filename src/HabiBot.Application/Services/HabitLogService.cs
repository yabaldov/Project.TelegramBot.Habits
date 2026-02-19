using FluentValidation;
using HabiBot.Application.DTOs;
using HabiBot.Domain.Entities;
using HabiBot.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace HabiBot.Application.Services;

/// <summary>
/// Реализация сервиса для работы с записями выполнения привычек
/// </summary>
public class HabitLogService : IHabitLogService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateHabitLogDto> _createValidator;
    private readonly ILogger<HabitLogService> _logger;

    public HabitLogService(
        IUnitOfWork unitOfWork,
        IValidator<CreateHabitLogDto> createValidator,
        ILogger<HabitLogService> logger)
    {
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _logger = logger;
    }

    public async Task<HabitLog> CreateLogAsync(CreateHabitLogDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Создание записи выполнения для привычки {HabitId}", dto.HabitId);

        // Валидация
        await _createValidator.ValidateAndThrowAsync(dto, cancellationToken);

        // Проверка существования привычки
        var habitExists = await _unitOfWork.Habits.ExistsAsync(dto.HabitId, cancellationToken);
        if (!habitExists)
        {
            _logger.LogWarning("Привычка {HabitId} не найдена", dto.HabitId);
            throw new InvalidOperationException($"Привычка с ID {dto.HabitId} не найдена");
        }

        // Создание записи
        var log = new HabitLog
        {
            HabitId = dto.HabitId,
            CompletedAt = dto.CompletedAt,
            Notes = dto.Notes
        };

        await _unitOfWork.HabitLogs.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Запись выполнения создана: {LogId} для привычки {HabitId}", log.Id, log.HabitId);

        return log;
    }

    public async Task<IEnumerable<HabitLog>> GetHabitLogsAsync(long habitId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Получение записей выполнения для привычки {HabitId}", habitId);
        return await _unitOfWork.HabitLogs.GetHabitLogsAsync(habitId, cancellationToken);
    }

    public async Task<IEnumerable<HabitLog>> GetHabitLogsByDateRangeAsync(
        long habitId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Получение записей выполнения для привычки {HabitId} за период {StartDate} - {EndDate}", 
            habitId, startDate, endDate);
        
        return await _unitOfWork.HabitLogs.GetHabitLogsByDateRangeAsync(habitId, startDate, endDate, cancellationToken);
    }

    public async Task<bool> IsCompletedTodayAsync(long habitId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Проверка выполнения привычки {HabitId} сегодня", habitId);
        
        var today = DateTime.UtcNow.Date;
        return await _unitOfWork.HabitLogs.IsCompletedOnDateAsync(habitId, today, cancellationToken);
    }
}
