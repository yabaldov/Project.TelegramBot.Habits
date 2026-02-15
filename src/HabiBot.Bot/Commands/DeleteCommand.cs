using HabiBot.Application.Services;
using HabiBot.Bot.StateManagement;
using HabiBot.Infrastructure.Models.Telegram;
using HabiBot.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HabiBot.Bot.Commands;

/// <summary>
/// Команда /delete - удаление привычки
/// </summary>
public class DeleteCommand : BotCommandBase
{
    private readonly IUserService _userService;
    private readonly IHabitService _habitService;

    public DeleteCommand(
        ITelegramApiClient telegramClient,
        IUserStateManager stateManager,
        IUserService userService,
        IHabitService habitService,
        ILogger<DeleteCommand> logger)
        : base(telegramClient, stateManager, logger)
    {
        _userService = userService;
        _habitService = habitService;
    }

    public override string Name => "delete";
    public override string Description => "Удалить привычку";

    public override async Task ExecuteAsync(Update update, CancellationToken cancellationToken = default)
    {
        var chatId = GetChatId(update);
        var userId = update.Message?.From?.Id;

        if (chatId == null || userId == null)
        {
            Logger.LogWarning("Получен Update без ChatId или UserId");
            return;
        }

        try
        {
            // Проверяем, зарегистрирован ли пользователь
            var user = await _userService.GetByTelegramIdAsync(userId.Value, cancellationToken);
            if (user == null)
            {
                await SendMessageAsync(chatId.Value,
                    "Вы не зарегистрированы. Используйте /start для регистрации.",
                    cancellationToken);
                return;
            }

            // Получаем список привычек
            var habits = (await _habitService.GetUserHabitsAsync(user.Id, cancellationToken)).ToList();

            if (habits.Count == 0)
            {
                await SendMessageAsync(chatId.Value,
                    "У вас пока нет привычек. Добавьте привычку командой /add",
                    cancellationToken);
                return;
            }

            // Создаем inline клавиатуру с привычками
            var keyboard = new InlineKeyboardMarkup
            {
                InlineKeyboard = habits.Select(h => new[]
                {
                    new InlineKeyboardButton
                    {
                        Text = h.Name,
                        CallbackData = $"delete:{h.Id}"
                    }
                })
            };

            await SendMessageAsync(chatId.Value,
                "Выберите привычку для удаления:",
                cancellationToken,
                replyMarkup: keyboard);

            Logger.LogInformation("Показан список привычек для удаления пользователю {UserId}", userId.Value);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка выполнения команды /delete для чата {ChatId}", chatId);
            await SendMessageAsync(chatId.Value,
                "Произошла ошибка при получении списка привычек.",
                cancellationToken);
        }
    }

    /// <summary>
    /// Обработка выбора привычки для удаления (запрос подтверждения)
    /// </summary>
    public async Task HandleHabitSelectedAsync(long chatId, long userId, long habitId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userService.GetByTelegramIdAsync(userId, cancellationToken);
            if (user == null)
            {
                await SendMessageAsync(chatId, "Пользователь не найден.", cancellationToken);
                return;
            }

            var habit = await _habitService.GetByIdAsync(habitId, cancellationToken);

            if (habit == null || habit.UserId != user.Id)
            {
                await SendMessageAsync(chatId, "Привычка не найдена.", cancellationToken);
                return;
            }

            // Создаем клавиатуру для подтверждения
            var keyboard = new InlineKeyboardMarkup
            {
                InlineKeyboard = new[]
                {
                    new[]
                    {
                        new InlineKeyboardButton
                        {
                            Text = "✅ Да, удалить",
                            CallbackData = $"deleteconfirm:yes:{habitId}"
                        },
                        new InlineKeyboardButton
                        {
                            Text = "❌ Отмена",
                            CallbackData = "deleteconfirm:no"
                        }
                    }
                }
            };

            await SendMessageAsync(chatId,
                $"Вы уверены, что хотите удалить привычку **{habit.Name}**?\n\n⚠️ Это действие нельзя отменить.",
                cancellationToken,
                replyMarkup: keyboard);

            Logger.LogInformation("Запрошено подтверждение удаления привычки {HabitId}", habitId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка при запросе подтверждения удаления привычки {HabitId}", habitId);
            await SendMessageAsync(chatId, "Произошла ошибка.", cancellationToken);
        }
    }

    /// <summary>
    /// Обработка подтверждения удаления
    /// </summary>
    public async Task HandleDeleteConfirmAsync(long chatId, long userId, string answer, long habitId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (answer == "no")
            {
                await SendMessageAsync(chatId, "Удаление отменено.", cancellationToken);
                return;
            }

            var user = await _userService.GetByTelegramIdAsync(userId, cancellationToken);
            if (user == null)
            {
                await SendMessageAsync(chatId, "Пользователь не найден.", cancellationToken);
                return;
            }

            var habit = await _habitService.GetByIdAsync(habitId, cancellationToken);
            if (habit == null || habit.UserId != user.Id)
            {
                await SendMessageAsync(chatId, "Привычка не найдена.", cancellationToken);
                return;
            }

            var habitName = habit.Name;
            await _habitService.DeleteHabitAsync(habitId, cancellationToken);

            await SendMessageAsync(chatId, $"✅ Привычка **{habitName}** успешно удалена.", cancellationToken);

            Logger.LogInformation("Привычка {HabitId} удалена пользователем {UserId}", habitId, userId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка при удалении привычки {HabitId}", habitId);
            await SendMessageAsync(chatId, "Произошла ошибка при удалении привычки.", cancellationToken);
        }
    }
}
