namespace HabiBot.Application.DTOs;

/// <summary>
/// DTO для статистики привычки
/// </summary>
public record HabitStatistics
{
    /// <summary>
    /// ID привычки
    /// </summary>
    public long HabitId { get; init; }

    /// <summary>
    /// Название привычки
    /// </summary>
    public string HabitName { get; init; } = string.Empty;

    /// <summary>
    /// Количество выполнений
    /// </summary>
    public int CompletedCount { get; init; }

    /// <summary>
    /// Ожидаемое количество выполнений (для процента)
    /// </summary>
    public int ExpectedCount { get; init; }

    /// <summary>
    /// Процент выполнения
    /// </summary>
    public double CompletionRate => ExpectedCount > 0 ? (double)CompletedCount / ExpectedCount * 100 : 0;

    /// <summary>
    /// Последнее выполнение
    /// </summary>
    public DateTime? LastCompleted { get; init; }

    /// <summary>
    /// Текущая серия (streak)
    /// </summary>
    public int CurrentStreak { get; init; }

    /// <summary>
    /// Лучшая серия
    /// </summary>
    public int BestStreak { get; init; }
}

/// <summary>
/// DTO для общей статистики пользователя
/// </summary>
public record UserStatistics
{
    /// <summary>
    /// Всего привычек
    /// </summary>
    public int TotalHabits { get; init; }

    /// <summary>
    /// Всего выполнений
    /// </summary>
    public int TotalCompletions { get; init; }

    /// <summary>
    /// Выполнений сегодня
    /// </summary>
    public int CompletionsToday { get; init; }

    /// <summary>
    /// Статистика по каждой привычке
    /// </summary>
    public IEnumerable<HabitStatistics> HabitStats { get; init; } = Enumerable.Empty<HabitStatistics>();

    /// <summary>
    /// Начало периода
    /// </summary>
    public DateTime PeriodStart { get; init; }

    /// <summary>
    /// Конец периода
    /// </summary>
    public DateTime PeriodEnd { get; init; }
}
