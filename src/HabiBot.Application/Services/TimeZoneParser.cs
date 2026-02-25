using System.Text.RegularExpressions;

namespace HabiBot.Application.Services;

/// <summary>
/// Парсер UTC-смещения из пользовательского ввода
/// </summary>
public static partial class TimeZoneParser
{
    /// <summary>
    /// Парсит строку UTC-смещения и возвращает нормализованный формат ("+03:00", "-05:30", "+00:00").
    /// Возвращает null при невалидном вводе.
    /// </summary>
    public static string? Parse(string input)
    {
        var trimmed = input.Trim();

        // Убираем необязательный префикс "UTC"
        if (trimmed.StartsWith("UTC", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[3..].Trim();
        }

        if (string.IsNullOrEmpty(trimmed) || trimmed == "0")
        {
            return "+00:00";
        }

        var match = OffsetRegex().Match(trimmed);
        if (!match.Success)
        {
            return null;
        }

        var sign = match.Groups["sign"].Value == "-" ? "-" : "+";
        var hours = int.Parse(match.Groups["hours"].Value);
        var minutes = match.Groups["minutes"].Success ? int.Parse(match.Groups["minutes"].Value) : 0;

        if (hours > 14 || minutes > 59 || (hours == 14 && minutes > 0))
        {
            return null;
        }

        return $"{sign}{hours:D2}:{minutes:D2}";
    }

    /// <summary>
    /// Создаёт TimeZoneInfo из нормализованной строки смещения ("+03:00").
    /// Возвращает null если формат невалиден.
    /// </summary>
    public static TimeZoneInfo? ToTimeZoneInfo(string? timeZone)
    {
        if (string.IsNullOrEmpty(timeZone))
        {
            return null;
        }

        // Пробуем как системный ID (обратная совместимость с IANA)
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            // Продолжаем — пробуем как UTC-смещение
        }

        if (!TimeSpan.TryParse(timeZone, out var offset))
        {
            return null;
        }

        return TimeZoneInfo.CreateCustomTimeZone($"UTC{timeZone}", offset, $"UTC{timeZone}", $"UTC{timeZone}");
    }

    [GeneratedRegex(@"^(?<sign>[+-]?)(?<hours>\d{1,2})(:(?<minutes>\d{2}))?$")]
    private static partial Regex OffsetRegex();
}
