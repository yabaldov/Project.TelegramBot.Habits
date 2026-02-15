using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HabiBot.Bot.StateManagement;

/// <summary>
/// Реализация менеджера состояний пользователей с использованием In-Memory Cache
/// </summary>
public class UserStateManager : IUserStateManager
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<UserStateManager> _logger;
    private readonly TimeSpan _stateExpiration = TimeSpan.FromHours(1);

    public UserStateManager(IMemoryCache cache, ILogger<UserStateManager> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public UserContext GetUserContext(long telegramId)
    {
        var key = GetCacheKey(telegramId);
        
        if (!_cache.TryGetValue(key, out UserContext? context) || context == null)
        {
            context = new UserContext { TelegramId = telegramId };
            SetUserContext(telegramId, context);
            _logger.LogDebug("Создан новый контекст для пользователя {TelegramId}", telegramId);
        }

        return context;
    }

    public void SetState(long telegramId, UserState state)
    {
        var context = GetUserContext(telegramId);
        context.State = state;
        context.LastUpdated = DateTime.UtcNow;
        SetUserContext(telegramId, context);
        
        _logger.LogDebug("Установлено состояние {State} для пользователя {TelegramId}", state, telegramId);
    }

    public UserState GetState(long telegramId)
    {
        var context = GetUserContext(telegramId);
        return context.State;
    }

    public void SetData(long telegramId, string key, object value)
    {
        var context = GetUserContext(telegramId);
        context.Data[key] = value;
        context.LastUpdated = DateTime.UtcNow;
        SetUserContext(telegramId, context);
        
        _logger.LogDebug("Сохранены данные {Key} для пользователя {TelegramId}", key, telegramId);
    }

    public T? GetData<T>(long telegramId, string key)
    {
        var context = GetUserContext(telegramId);
        
        if (context.Data.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }

        return default;
    }

    public void ClearState(long telegramId)
    {
        var context = GetUserContext(telegramId);
        context.State = UserState.None;
        context.Data.Clear();
        context.LastUpdated = DateTime.UtcNow;
        SetUserContext(telegramId, context);
        
        _logger.LogDebug("Очищено состояние для пользователя {TelegramId}", telegramId);
    }

    private void SetUserContext(long telegramId, UserContext context)
    {
        var key = GetCacheKey(telegramId);
        var options = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(_stateExpiration);
        
        _cache.Set(key, context, options);
    }

    private static string GetCacheKey(long telegramId) => $"user_state_{telegramId}";
}
