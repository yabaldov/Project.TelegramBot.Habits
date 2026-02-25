using HabiBot.Infrastructure.Models.Telegram;

namespace HabiBot.Bot.Commands;

/// <summary>
/// Хелпер для создания Reply-клавиатур бота
/// </summary>
public static class ReplyKeyboardHelper
{
    /// <summary>
    /// Reply-клавиатура для незарегистрированного пользователя
    /// </summary>
    public static ReplyKeyboardMarkup PreRegistrationKeyboard => new()
    {
        Keyboard = new[]
        {
            new[] { new KeyboardButton { Text = "/start" } }
        },
        ResizeKeyboard = true
    };

    /// <summary>
    /// Reply-клавиатура для зарегистрированного пользователя
    /// </summary>
    public static ReplyKeyboardMarkup PostRegistrationKeyboard => new()
    {
        Keyboard = new[]
        {
            new[]
            {
                new KeyboardButton { Text = "/add" },
                new KeyboardButton { Text = "/summary" },
                new KeyboardButton { Text = "/list" }
            }
        },
        ResizeKeyboard = true
    };
}
