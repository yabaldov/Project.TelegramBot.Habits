namespace HabiBot.Domain.Enums;

/// <summary>
/// Частота выполнения привычки
/// </summary>
public enum HabitFrequency
{
    /// <summary>
    /// Каждый день
    /// </summary>
    Daily = 1,

    /// <summary>
    /// Несколько раз в неделю
    /// </summary>
    Weekly = 2,

    /// <summary>
    /// Пользовательское расписание
    /// </summary>
    Custom = 3
}
