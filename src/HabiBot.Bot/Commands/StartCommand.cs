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

            // Создаем пользователя
            var telegramUser = update.Message?.From;
            
            var createUserDto = new CreateUserDto
            {
                Name = name.Trim(),
                TelegramUserId = userId.Value,
                TelegramChatId = chatId.Value,
                TelegramFirstName = telegramUser?.FirstName,
                TelegramLastName = telegramUser?.LastName,
                TelegramUserName = telegramUser?.Username
            };

            await _userService.CreateUserAsync(createUserDto, cancellationToken);

            await SendMessageAsync(chatId.Value, 
                $"Отлично, {name}! ✅\n\n" +
                "Регистрация завершена. Теперь ты можешь:\n" +
                "• /add - добавить новую привычку\n" +
                "• /list - посмотреть свои привычки\n" +
                "• /stats - посмотреть статистику", 
                cancellationToken,
                replyMarkup: ReplyKeyboardHelper.PostRegistrationKeyboard);

            StateManager.ClearState(userId.Value);
            Logger.LogInformation("Пользователь {UserId} успешно зарегистрирован с именем {Name}", userId.Value, name);
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
