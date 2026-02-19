using HabiBot.Bot.Commands;
using HabiBot.Bot.Middleware;
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
    private readonly RateLimitingService _rateLimiting;
    private readonly Dictionary<string, IBotCommand> _commands;
    private readonly StartCommand _startCommand;
    private readonly AddCommand _addCommand;
    private readonly EditCommand _editCommand;
    private readonly DeleteCommand _deleteCommand;
    private readonly CompletedHandler _completedHandler;

    public CommandRouter(
        ILogger<CommandRouter> logger,
        IUserStateManager stateManager,
        ITelegramApiClient telegramClient,
        RateLimitingService rateLimiting,
        StartCommand startCommand,
        HelpCommand helpCommand,
        ListCommand listCommand,
        AddCommand addCommand,
        StatsCommand statsCommand,
        EditCommand editCommand,
        DeleteCommand deleteCommand,
        CompletedHandler completedHandler)
    {
        _logger = logger;
        _stateManager = stateManager;
        _telegramClient = telegramClient;
        _rateLimiting = rateLimiting;
        _startCommand = startCommand;
        _addCommand = addCommand;
        _editCommand = editCommand;
        _deleteCommand = deleteCommand;
        _completedHandler = completedHandler;

        _commands = new Dictionary<string, IBotCommand>(StringComparer.OrdinalIgnoreCase)
        {
            { startCommand.Name, startCommand },
            { helpCommand.Name, helpCommand },
            { listCommand.Name, listCommand },
            { addCommand.Name, addCommand },
            { statsCommand.Name, statsCommand },
            { editCommand.Name, editCommand },
            { deleteCommand.Name, deleteCommand },
        };
    }

    /// <summary>
    /// Обработать входящее обновление
    /// </summary>
    public async Task RouteAsync(Update update, CancellationToken cancellationToken = default)
    {
        // Обработка callback query от inline кнопок
        if (update.CallbackQuery != null)
        {
            await HandleCallbackQueryAsync(update.CallbackQuery, cancellationToken);
            return;
        }

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

        // Проверяем Rate Limiting
        if (!_rateLimiting.TryAcquire(userId.Value, out var retryAfter))
        {
            _logger.LogWarning("Rate limit exceeded для пользователя {UserId}. Retry after: {RetryAfter}", 
                userId.Value, retryAfter);
            
            var chatId = update.Message?.Chat.Id;
            if (chatId.HasValue)
            {
                var retryMinutes = (int)Math.Ceiling(retryAfter!.Value.TotalMinutes);
                await SendMessageAsync(chatId.Value, 
                    $"⚠️ Превышен лимит запросов. Попробуйте через {retryMinutes} мин.", 
                    cancellationToken);
            }
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

            case UserState.WaitingForEditName:
                var chatId1 = update.Message?.Chat.Id ?? 0;
                var userId1 = update.Message?.From?.Id ?? 0;
                await _editCommand.HandleNameInputAsync(chatId1, userId1, messageText, cancellationToken);
                break;

            case UserState.WaitingForEditTime:
                var chatId2 = update.Message?.Chat.Id ?? 0;
                var userId2 = update.Message?.From?.Id ?? 0;
                await _editCommand.HandleTimeInputAsync(chatId2, userId2, messageText, cancellationToken);
                break;

            default:
                _logger.LogWarning("Неизвестное состояние {State} для пользователя {UserId}", state, userId);
                _stateManager.ClearState(userId);
                break;
        }
    }

    /// <summary>
    /// Обработка callback query от inline кнопок
    /// </summary>
    private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        try
        {
            var chatId = callbackQuery.Message?.Chat.Id ?? 0;
            var userId = callbackQuery.From.Id;
            var data = callbackQuery.Data ?? string.Empty;

            _logger.LogInformation("Получен callback query от пользователя {UserId}: {Data}", userId, data);

            // Отвечаем на callback query чтобы убрать "часики" в Telegram
            await _telegramClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);

            var parts = data.Split(':');
            if (parts.Length < 2)
            {
                _logger.LogWarning("Некорректный формат callback data: {Data}", data);
                return;
            }

            var action = parts[0];
            
            switch (action)
            {
                case "edit":
                    if (long.TryParse(parts[1], out var editHabitId))
                    {
                        await _editCommand.HandleHabitSelectedAsync(chatId, userId, editHabitId, cancellationToken);
                    }
                    break;

                case "editfield":
                    if (parts.Length >= 3 && long.TryParse(parts[2], out var fieldHabitId))
                    {
                        await _editCommand.HandleFieldSelectedAsync(chatId, userId, parts[1], fieldHabitId, cancellationToken);
                    }
                    else if (parts[1] == "cancel")
                    {
                        await _editCommand.HandleFieldSelectedAsync(chatId, userId, "cancel", 0, cancellationToken);
                    }
                    break;

                case "frequency":
                    if (parts.Length >= 3 && long.TryParse(parts[2], out var freqHabitId))
                    {
                        await _editCommand.HandleFrequencySelectedAsync(chatId, userId, parts[1], freqHabitId, cancellationToken);
                    }
                    break;

                case "delete":
                    if (long.TryParse(parts[1], out var deleteHabitId))
                    {
                        await _deleteCommand.HandleHabitSelectedAsync(chatId, userId, deleteHabitId, cancellationToken);
                    }
                    break;

                case "deleteconfirm":
                    if (parts.Length >= 3 && long.TryParse(parts[2], out var confirmHabitId))
                    {
                        await _deleteCommand.HandleDeleteConfirmAsync(chatId, userId, parts[1], confirmHabitId, cancellationToken);
                    }
                    else if (parts[1] == "no")
                    {
                        await _deleteCommand.HandleDeleteConfirmAsync(chatId, userId, "no", 0, cancellationToken);
                    }
                    break;

                default:
                    _logger.LogWarning("Неизвестный callback action: {Action}", action);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки callback query");
        }
    }

    private async Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken)
    {
        try
        {
            var request = new SendMessageRequest
            {
                ChatId = chatId,
                Text = text
            };
            await _telegramClient.SendMessageAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отправки сообщения в чат {ChatId}", chatId);
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
