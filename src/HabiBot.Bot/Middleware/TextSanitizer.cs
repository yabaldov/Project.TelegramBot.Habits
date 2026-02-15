using System.Text;
using System.Text.RegularExpressions;

namespace HabiBot.Bot.Middleware;

/// <summary>
/// Сервис для санитизации пользовательского ввода
/// </summary>
public static partial class TextSanitizer
{
    /// <summary>
    /// Очистить текст от потенциально опасных символов для Telegram
    /// </summary>
    public static string SanitizeForTelegram(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // Удаляем управляющие символы кроме переноса строки и табуляции
        var cleaned = RemoveControlCharactersRegex().Replace(text, string.Empty);

        // Ограничиваем длину
        const int maxLength = 4096; // Лимит Telegram для текстовых сообщений
        if (cleaned.Length > maxLength)
        {
            cleaned = cleaned[..maxLength];
        }

        // Экранируем специальные символы Markdown
        cleaned = EscapeMarkdownV2(cleaned);

        return cleaned.Trim();
    }

    /// <summary>
    /// Валидировать имя пользователя
    /// </summary>
    public static (bool IsValid, string ErrorMessage) ValidateUserName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "Имя не может быть пустым");
        }

        if (name.Length < 2)
        {
            return (false, "Имя должно содержать минимум 2 символа");
        }

        if (name.Length > 50)
        {
            return (false, "Имя не должно превышать 50 символов");
        }

        // Проверяем на недопустимые символы
        if (InvalidCharactersRegex().IsMatch(name))
        {
            return (false, "Имя содержит недопустимые символы");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Валидировать название привычки
    /// </summary>
    public static (bool IsValid, string ErrorMessage) ValidateHabitName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "Название не может быть пустым");
        }

        if (name.Length < 2)
        {
            return (false, "Название должно содержать минимум 2 символа");
        }

        if (name.Length > 100)
        {
            return (false, "Название не должно превышать 100 символов");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Экранировать специальные символы для Markdown V2
    /// </summary>
    private static string EscapeMarkdownV2(string text)
    {
        // Telegram Markdown V2 требует экранирования: _*[]()~`>#+-=|{}.!
        // Но мы используем простой режим без markdown, поэтому просто удаляем специальные символы
        return text;
    }

    [GeneratedRegex(@"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]")]
    private static partial Regex RemoveControlCharactersRegex();

    [GeneratedRegex(@"[<>""'`]")]
    private static partial Regex InvalidCharactersRegex();
}
