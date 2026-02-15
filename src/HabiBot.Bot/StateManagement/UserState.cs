namespace HabiBot.Bot.StateManagement;

/// <summary>
/// Состояния диалога пользователя
/// </summary>
public enum UserState
{
    /// <summary>
    /// Нет активного состояния
    /// </summary>
    None,

    /// <summary>
    /// Ожидание ввода имени при регистрации
    /// </summary>
    WaitingForName,

    /// <summary>
    /// Ожидание ввода названия привычки
    /// </summary>
    WaitingForHabitName,

    /// <summary>
    /// Ожидание ввода времени напоминания
    /// </summary>
    WaitingForReminderTime
}
