namespace HabiBot.Bot.StateManagement;

/// <summary>
/// Интерфейс для управления состоянием пользователей
/// </summary>
public interface IUserStateManager
{
    /// <summary>
    /// Получить контекст пользователя
    /// </summary>
    UserContext GetUserContext(long telegramId);

    /// <summary>
    /// Установить состояние пользователя
    /// </summary>
    void SetState(long telegramId, UserState state);

    /// <summary>
    /// Получить состояние пользователя
    /// </summary>
    UserState GetState(long telegramId);

    /// <summary>
    /// Сохранить данные в контексте
    /// </summary>
    void SetData(long telegramId, string key, object value);

    /// <summary>
    /// Получить данные из контекста
    /// </summary>
    T? GetData<T>(long telegramId, string key);

    /// <summary>
    /// Очистить состояние пользователя
    /// </summary>
    void ClearState(long telegramId);
}
