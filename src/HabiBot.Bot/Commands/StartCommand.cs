using HabiBot.Application.DTOs;
using HabiBot.Application.Services;
using HabiBot.Bot.StateManagement;
using HabiBot.Infrastructure.Models.Telegram;
using HabiBot.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HabiBot.Bot.Commands;

/// <summary>
/// Команда /start - регистрация нового пользователя
/// </summary>
public class StartCommand : BotCommandBase
{
    private readonly IUserService _userService;

    public StartCommand(
        ITelegramApiClient telegramClient,
        IUserStateManager stateManager,
        IUserService userService,
        ILogger<StartCommand> logger) 
        : base(telegramClient, stateManager, logger)
    {
        _userService = userService;
    }

    public override string Name => "start";
    public override string Description => "Начать работу с ботом";

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
            // Проверяем, зарегистрирован ли пользователь
            var existingUser = await _userService.GetByTelegramIdAsync(userId.Value, cancellationToken);

            if (existingUser != null)
            {
                // Пользователь уже зарегистрирован
                await SendMessageAsync(chatId.Value, 
                    $"Привет, {existingUser.Name}! 👋\n\n" +
                    "Ты уже зарегистрирован. Используй /help для просмотра доступных команд.", 
                    cancellationToken,
                    replyMarkup: ReplyKeyboardHelper.PostRegistrationKeyboard);
                
                StateManager.ClearState(userId.Value);
                return;
            }

            // Начинаем процесс регистрации
            await SendMessageAsync(chatId.Value, 
                "Привет! 👋 Я бот для отслеживания привычек.\n\n" +
                "Как мне тебя называть?", 
                cancellationToken);

            StateManager.SetState(userId.Value, UserState.WaitingForName);
            Logger.LogInformation("Начата регистрация пользователя {UserId}", userId.Value);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка выполнения команды /start для пользователя {UserId}", userId);
            await SendMessageAsync(chatId.Value, 
                "Произошла ошибка. Попробуй позже.", 
                cancellationToken);
        }
    }

    /// <summary>
    /// Обработка ответа с именем пользователя
    /// </summary>
    public async Task HandleNameInputAsync(Update update, string name, CancellationToken cancellationToken = default)
    {
        var chatId = GetChatId(update);
        var userId = GetUserId(update);

        if (chatId == null || userId == null)
        {
            return;
        }

        try
        {
            // Валидация имени
            if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            {
                await SendMessageAsync(chatId.Value, 
                    "Имя должно содержать от 1 до 100 символов. Попробуй ещё раз:", 
                    cancellationToken);
                return;
            }

            // Сохраняем имя и данные Telegram в контексте, переходим к вводу часового пояса
            var telegramUser = update.Message?.From;
            StateManager.SetData(userId.Value, "RegistrationName", name.Trim());
            StateManager.SetData(userId.Value, "TelegramFirstName", telegramUser?.FirstName ?? string.Empty);
            StateManager.SetData(userId.Value, "TelegramLastName", telegramUser?.LastName ?? string.Empty);
            StateManager.SetData(userId.Value, "TelegramUserName", telegramUser?.Username ?? string.Empty);

            await SendMessageAsync(chatId.Value,
                "Укажи свой часовой пояс в виде смещения от UTC.\n\n" +
                "Примеры: +3, -5, +5:30, 0\n" +
                "(Москва = +3, Киев = +2, Лондон = 0)",
                cancellationToken);

            StateManager.SetState(userId.Value, UserState.WaitingForTimeZone);
            Logger.LogInformation("Пользователь {UserId} ввёл имя, ожидание часового пояса", userId.Value);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка обработки имени для пользователя {UserId}", userId);
            await SendMessageAsync(chatId.Value, 
                "Произошла ошибка при регистрации. Попробуй начать заново с /start", 
                cancellationToken);
            StateManager.ClearState(userId.Value);
        }
    }

    /// <summary>
    /// Обработка ответа с часовым поясом
    /// </summary>
    public async Task HandleTimeZoneInputAsync(Update update, string input, CancellationToken cancellationToken = default)
    {
        var chatId = GetChatId(update);
        var userId = GetUserId(update);

        if (chatId == null || userId == null)
        {
            return;
        }

        try
        {
            var timeZone = TimeZoneParser.Parse(input);
            if (timeZone == null)
            {
                await SendMessageAsync(chatId.Value,
                    "Неверный формат часового пояса. ⚠️\n\n" +
                    "Введи смещение от UTC, например: +3, -5, +5:30, 0",
                    cancellationToken);
                return;
            }

            // Восстанавливаем данные регистрации из контекста
            var name = StateManager.GetData<string>(userId.Value, "RegistrationName");
            if (string.IsNullOrEmpty(name))
            {
                await SendMessageAsync(chatId.Value,
                    "Произошла ошибка. Попробуй начать заново с /start",
                    cancellationToken);
                StateManager.ClearState(userId.Value);
                return;
            }

            var telegramFirstName = StateManager.GetData<string>(userId.Value, "TelegramFirstName");
            var telegramLastName = StateManager.GetData<string>(userId.Value, "TelegramLastName");
            var telegramUserName = StateManager.GetData<string>(userId.Value, "TelegramUserName");

            var createUserDto = new CreateUserDto
            {
                Name = name,
                TelegramUserId = userId.Value,
                TelegramChatId = chatId.Value,
                TelegramFirstName = string.IsNullOrEmpty(telegramFirstName) ? null : telegramFirstName,
                TelegramLastName = string.IsNullOrEmpty(telegramLastName) ? null : telegramLastName,
                TelegramUserName = string.IsNullOrEmpty(telegramUserName) ? null : telegramUserName,
                TimeZone = timeZone
            };

            await _userService.CreateUserAsync(createUserDto, cancellationToken);

            await SendMessageAsync(chatId.Value, 
                $"Отлично, {name}! ✅\n" +
                $"Часовой пояс: UTC{timeZone}\n\n" +
                "Регистрация завершена. Теперь ты можешь:\n" +
                "• /add - добавить новую привычку\n" +
                "• /list - посмотреть свои привычки\n" +
                "• /stats - посмотреть статистику", 
                cancellationToken,
                replyMarkup: ReplyKeyboardHelper.PostRegistrationKeyboard);

            StateManager.ClearState(userId.Value);
            Logger.LogInformation("Пользователь {UserId} зарегистрирован с именем {Name}, TZ={TimeZone}", userId.Value, name, timeZone);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка регистрации пользователя {UserId}", userId);
            await SendMessageAsync(chatId.Value, 
                "Произошла ошибка при регистрации. Попробуй начать заново с /start", 
                cancellationToken);
            StateManager.ClearState(userId.Value);
        }
    }
}
