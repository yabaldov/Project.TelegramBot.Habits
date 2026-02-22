using HabiBot.Application.Services;
using HabiBot.Bot.StateManagement;
using HabiBot.Infrastructure.Models.Telegram;
using HabiBot.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HabiBot.Bot.Commands;

/// <summary>
/// Команда /settimezone - установить часовой пояс
/// </summary>
public class SetTimezoneCommand : BotCommandBase
{
    private readonly IUserService _userService;

    public SetTimezoneCommand(
        ITelegramApiClient telegramClient,
        IUserStateManager stateManager,
        IUserService userService,
        ILogger<SetTimezoneCommand> logger)
        : base(telegramClient, stateManager, logger)
    {
        _userService = userService;
    }

    public override string Name => "settimezone";
    public override string Description => "Установить часовой пояс";

    public override async Task ExecuteAsync(Update update, CancellationToken cancellationToken = default)
    {
        var chatId = GetChatId(update);
        var userId = GetUserId(update);

        if (chatId == null || userId == null)
        {
            Logger.LogWarning("Получен Update без ChatId или UserId");
            return;
        }

        try
        {
            var user = await _userService.GetByTelegramIdAsync(userId.Value, cancellationToken);

            if (user == null)
            {
                await SendMessageAsync(chatId.Value,
                    "Ты не зарегистрирован. Используй /start для регистрации.",
                    cancellationToken,
                    replyMarkup: ReplyKeyboardHelper.PreRegistrationKeyboard);
                return;
            }

            var currentTz = string.IsNullOrEmpty(user.TimeZone) ? "не установлен" : $"UTC{user.TimeZone}";

            await SendMessageAsync(chatId.Value,
                $"🕐 Текущий часовой пояс: {currentTz}\n\n" +
                "Введи новый часовой пояс в виде смещения от UTC.\n" +
                "Примеры: +3, -5, +5:30, 0\n" +
                "(Москва = +3, Киев = +2, Лондон = 0)",
                cancellationToken);

            StateManager.SetData(userId.Value, "UserId", user.Id);
            StateManager.SetState(userId.Value, UserState.WaitingForTimeZone);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка выполнения команды /settimezone для пользователя {UserId}", userId);
            await SendMessageAsync(chatId.Value,
                "Произошла ошибка. Попробуй позже.",
                cancellationToken);
        }
    }

    /// <summary>
    /// Обработка ввода часового пояса (для зарегистрированных пользователей)
    /// </summary>
    public async Task HandleTimeZoneInputAsync(long chatId, long userId, string input, CancellationToken cancellationToken = default)
    {
        try
        {
            var timeZone = TimeZoneParser.Parse(input);
            if (timeZone == null)
            {
                await SendMessageAsync(chatId,
                    "Неверный формат часового пояса. ⚠️\n\n" +
                    "Введи смещение от UTC, например: +3, -5, +5:30, 0",
                    cancellationToken);
                return;
            }

            var userIdDb = StateManager.GetData<long>(userId, "UserId");
            if (userIdDb == 0)
            {
                await SendMessageAsync(chatId,
                    "Произошла ошибка. Попробуй начать заново с /settimezone",
                    cancellationToken);
                StateManager.ClearState(userId);
                return;
            }

            await _userService.UpdateTimeZoneAsync(userIdDb, timeZone, cancellationToken);

            await SendMessageAsync(chatId,
                $"Часовой пояс обновлён: UTC{timeZone} ✅",
                cancellationToken);

            StateManager.ClearState(userId);
            Logger.LogInformation("Часовой пояс обновлён для пользователя {UserId}: {TimeZone}", userId, timeZone);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка обработки часового пояса для пользователя {UserId}", userId);
            await SendMessageAsync(chatId,
                "Произошла ошибка. Попробуй позже.",
                cancellationToken);
            StateManager.ClearState(userId);
        }
    }
}
