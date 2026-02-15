using System.Collections.Concurrent;

namespace HabiBot.Bot.Middleware;

/// <summary>
/// Настройки для Rate Limiting
/// </summary>
public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Максимальное количество запросов в минуту от одного пользователя
    /// </summary>
    public int MaxRequestsPerMinute { get; set; } = 20;

    /// <summary>
    /// Длительность бана в минутах при превышении лимита
    /// </summary>
    public int BanDurationMinutes { get; set; } = 5;
}

/// <summary>
/// Сервис для ограничения частоты запросов от пользователей
/// </summary>
public class RateLimitingService
{
    private readonly RateLimitingOptions _options;
    private readonly ConcurrentDictionary<long, UserRateLimitInfo> _userRequests = new();

    public RateLimitingService(Microsoft.Extensions.Options.IOptions<RateLimitingOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Проверить, можно ли обработать запрос от пользователя
    /// </summary>
    public bool TryAcquire(long userId, out TimeSpan? retryAfter)
    {
        var now = DateTime.UtcNow;
        var info = _userRequests.GetOrAdd(userId, _ => new UserRateLimitInfo());

        lock (info)
        {
            // Проверяем, забанен ли пользователь
            if (info.BannedUntil.HasValue && info.BannedUntil.Value > now)
            {
                retryAfter = info.BannedUntil.Value - now;
                return false;
            }

            // Сбрасываем бан, если время истекло
            if (info.BannedUntil.HasValue && info.BannedUntil.Value <= now)
            {
                info.BannedUntil = null;
                info.Requests.Clear();
            }

            // Удаляем старые запросы (старше 1 минуты)
            var oneMinuteAgo = now.AddMinutes(-1);
            info.Requests.RemoveAll(r => r < oneMinuteAgo);

            // Проверяем лимит
            if (info.Requests.Count >= _options.MaxRequestsPerMinute)
            {
                info.BannedUntil = now.AddMinutes(_options.BanDurationMinutes);
                retryAfter = TimeSpan.FromMinutes(_options.BanDurationMinutes);
                return false;
            }

            // Добавляем новый запрос
            info.Requests.Add(now);
            retryAfter = null;
            return true;
        }
    }

    /// <summary>
    /// Получить количество оставшихся запросов для пользователя
    /// </summary>
    public int GetRemainingRequests(long userId)
    {
        if (!_userRequests.TryGetValue(userId, out var info))
        {
            return _options.MaxRequestsPerMinute;
        }

        lock (info)
        {
            var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
            info.Requests.RemoveAll(r => r < oneMinuteAgo);
            return Math.Max(0, _options.MaxRequestsPerMinute - info.Requests.Count);
        }
    }

    /// <summary>
    /// Очистить устаревшие данные (вызывать периодически)
    /// </summary>
    public void Cleanup()
    {
        var now = DateTime.UtcNow;
        var keysToRemove = new List<long>();

        foreach (var kvp in _userRequests)
        {
            lock (kvp.Value)
            {
                var oneMinuteAgo = now.AddMinutes(-1);
                kvp.Value.Requests.RemoveAll(r => r < oneMinuteAgo);

                // Если пользователь не банен и нет запросов - удаляем
                if (!kvp.Value.BannedUntil.HasValue && kvp.Value.Requests.Count == 0)
                {
                    keysToRemove.Add(kvp.Key);
                }

                // Если бан истек и нет запросов - удаляем
                if (kvp.Value.BannedUntil.HasValue && kvp.Value.BannedUntil.Value < now && kvp.Value.Requests.Count == 0)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
        }

        foreach (var key in keysToRemove)
        {
            _userRequests.TryRemove(key, out _);
        }
    }
}

/// <summary>
/// Информация о запросах пользователя для Rate Limiting
/// </summary>
internal class UserRateLimitInfo
{
    public List<DateTime> Requests { get; } = new();
    public DateTime? BannedUntil { get; set; }
}
