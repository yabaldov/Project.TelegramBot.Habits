using HabiBot.Bot.Commands;
using HabiBot.Bot.StateManagement;
using HabiBot.Infrastructure.Models.Telegram;
using HabiBot.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HabiBot.Bot.Services;

/// <summary>
/// Маршрутизатор команд бота
/// </summary>
public class CommandRouter
{
    private readonly ILogger<CommandRouter> _logger;
    private readonly IUserStateManager _stateManager;
    private readonly ITelegramApiClient _telegramClient;
    private readonly Dictionary<string, IBotCommand> _commands;
    private readonly StartCommand _startCommand;
    private readonly AddCommand _addCommand;
    private readonly CompletedHandler _completedHandler;

    public CommandRouter(
        ILogger<CommandRouter> logger,
        IUserStateManager stateManager,
        ITelegramApiClient telegramClient,
        StartCommand startCommand,
        HelpCommand helpCommand,
        ListCommand listCommand,
        AddCommand addCommand,
        StatsCommand statsCommand,
        CompletedHandler completedHandler)
    {
        _logger = logger;
        _stateManager = stateManager;
        _telegramClient = telegramClient;
        _startCommand = startCommand;
        _addCommand = addCommand;
        _completedHandler = completedHandler;

        _commands = new Dictionary<string, IBotCommand>(StringComparer.OrdinalIgnoreCase)
        {
            { startCommand.Name, startCommand },
            { helpCommand.Name, helpCommand },
            { listCommand.Name, listCommand },
            { addCommand.Name, addCommand },
            { statsCommand.Name, statsCommand },
        };
    }

    /// <summary>
    /// Обработать входящее обновление
    /// </summary>
    public async Task RouteAsync(Update update, CancellationToken cancellationToken = default)
    {
        if (update.Message == null)
        {
            _logger.LogDebug("Получен Update без Message");
            return;
        }

        var messageText = update.Message.Text?.Trim();
        var userId = update.Message.From?.Id;

        if (string.IsNullOrEmpty(messageText) || userId == null)
        {
            _logger.LogDebug("Получено сообщение без текста или UserId");
            return;
        }

        try
        {
            // Проверяем состояние пользователя
            var userState = _stateManager.GetState(userId.Value);

            // Если пользователь в процессе многошагового диалога
            if (userState != UserState.None)
            {
                await HandleStateBasedInputAsync(update, messageText, userId.Value, userState, cancellationToken);
                return;
            }

            // Проверяем, является ли это командой (начинается с /)
            if (messageText.StartsWith('/'))
            {
                var commandName = ExtractCommandName(messageText);
                
                if (_commands.TryGetValue(commandName, out var command))
                {
                    _logger.LogInformation("Выполняется команда /{Command} для пользователя {UserId}", commandName, userId.Value);
                    await command.ExecuteAsync(update, cancellationToken);
                }
                else
                {
                    _logger.LogWarning("Неизвестная команда /{Command} от пользователя {UserId}", commandName, userId.Value);
                    
                    // Отправляем сообщение о неизвестной команде
                    var chatId = update.Message?.Chat.Id;
                    if (chatId.HasValue)
                    {
                        await SendUnknownCommandMessageAsync(chatId.Value, cancellationToken);
                    }
                }
                return;
            }

            // Проверяем, является ли это отметкой выполнения
            if (messageText.StartsWith("Выполнено!", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Обработка отметки выполнения от пользователя {UserId}", userId.Value);
                await _completedHandler.ExecuteAsync(update, cancellationToken);
                return;
            }

            // Неизвестный текст
            _logger.LogDebug("Получен неизвестный текст от пользователя {UserId}: {Text}", userId.Value, messageText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка маршрутизации сообщения от пользователя {UserId}", userId);
        }
    }

    private async Task HandleStateBasedInputAsync(
        Update update, 
        string messageText, 
        long userId, 
        UserState state, 
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Обработка ввода в состоянии {State} для пользователя {UserId}", state, userId);

        switch (state)
        {
            case UserState.WaitingForName:
                await _startCommand.HandleNameInputAsync(update, messageText, cancellationToken);
                break;

            case UserState.WaitingForHabitName:
                await _addCommand.HandleHabitNameInputAsync(update, messageText, cancellationToken);
                break;

            case UserState.WaitingForReminderTime:
                await _addCommand.HandleReminderTimeInputAsync(update, messageText, cancellationToken);
                break;

            default:
                _logger.LogWarning("Неизвестное состояние {State} для пользователя {UserId}", state, userId);
                _stateManager.ClearState(userId);
                break;
        }
    }

    private static string ExtractCommandName(string messageText)
    {
        // Извлекаем название команды без / и аргументов
        var command = messageText.TrimStart('/').Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return command ?? string.Empty;
    }

    private async Task SendUnknownCommandMessageAsync(long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var request = new SendMessageRequest
            {
                ChatId = chatId,
                Text = "Введена неизвестная команда, введите /help для списка доступных команд."
            };

            await _telegramClient.SendMessageAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отправки сообщения о неизвестной команде в чат {ChatId}", chatId);
        }
    }
}
