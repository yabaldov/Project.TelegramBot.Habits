namespace HabiBot.Application.Services;

/// <summary>
/// Интерфейс сервиса для генерации ежедневной сводки по привычкам
/// </summary>
public interface IDailySummaryService
{
    /// <summary>
    /// Получить сводку по привычкам пользователя за день
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="date">Дата, за которую нужна сводка</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Объект с данными сводки</returns>
    Task<DailySummaryData> GetDailySummaryAsync(long userId, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Сгенерировать текст ежедневной сводки для отправки в Telegram
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="date">Дата сводки</param>
    /// <param name="includeNextDay">Включать ли план на следующий день</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Отформатированный текст сводки</returns>
    Task<string> GenerateSummaryTextAsync(long userId, DateOnly date, bool includeNextDay = true, CancellationToken cancellationToken = default);
}

/// <summary>
/// Данные ежедневной сводки
/// </summary>
public class DailySummaryData
{
    /// <summary>
    /// ID пользователя
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Дата сводки
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Общее количество запланированных привычек на этот день
    /// </summary>
    public int PlannedHabitsCount { get; set; }

    /// <summary>
    /// Количество выполненных привычек
    /// </summary>
    public int CompletedHabitsCount { get; set; }

    /// <summary>
    /// Процент выполнения (0-100)
    /// </summary>
    public int CompletionPercentage { get; set; }

    /// <summary>
    /// Список выполненных привычек с временем выполнения
    /// </summary>
    public List<CompletedHabitInfo> CompletedHabits { get; set; } = new();

    /// <summary>
    /// Список невыполненных привычек
    /// </summary>
    public List<UncompletedHabitInfo> UncompletedHabits { get; set; } = new();

    /// <summary>
    /// Список привычек, запланированных на следующий день
    /// </summary>
    public List<PlannedHabitInfo> NextDayHabits { get; set; } = new();

    /// <summary>
    /// Список привычек, которые можно еще выполнить сегодня
    /// </summary>
    public List<UncompletedHabitInfo> AvailableToCompleteToday { get; set; } = new();
}

/// <summary>
/// Информация о выполненной привычке
/// </summary>
public class CompletedHabitInfo
{
    public string HabitName { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
}

/// <summary>
/// Информация о невыполненной привычке
/// </summary>
public class UncompletedHabitInfo
{
    public long HabitId { get; set; }
    public string HabitName { get; set; } = string.Empty;
    public string? ScheduledTime { get; set; }
}

/// <summary>
/// Информация о планируемой привычке на следующий день
/// </summary>
public class PlannedHabitInfo
{
    public string HabitName { get; set; } = string.Empty;
    public string ScheduledTime { get; set; } = string.Empty;
}
