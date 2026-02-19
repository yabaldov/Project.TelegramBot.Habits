using HabiBot.Application.DTOs;
using HabiBot.Application.Services;
using HabiBot.Bot.StateManagement;
using HabiBot.Infrastructure.Models.Telegram;
using HabiBot.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HabiBot.Bot.Commands;

/// <summary>
/// Обработчик отметок выполнения привычки
/// </summary>
public class CompletedHandler : BotCommandBase
{
    private readonly IUserService _userService;
    private readonly IHabitService _habitService;
    private readonly IHabitLogService _habitLogService;

    public CompletedHandler(
        ITelegramApiClient telegramClient,
        IUserStateManager stateManager,
        IUserService userService,
        IHabitService habitService,
        IHabitLogService habitLogService,
        ILogger<CompletedHandler> logger)
        : base(telegramClient, stateManager, logger)
    {
        _userService = userService;
        _habitService = habitService;
        _habitLogService = habitLogService;
    }

    public override string Name => "completed";
    public override string Description => "Отметить привычку как выполненную";

    public override async Task ExecuteAsync(Update update, CancellationToken cancellationToken = default)
    {
        var chatId = GetChatId(update);
        var userId = GetUserId(update);
        var messageText = GetMessageText(update);

        if (chatId == null || userId == null || string.IsNullOrWhiteSpace(messageText))
        {
            Logger.LogWarning("Получен Update без ChatId, UserId или текста");
            return;
        }

        try
        {
            // Проверяем регистрацию
            var user = await _userService.GetByTelegramIdAsync(userId.Value, cancellationToken);

            if (user == null)
            {
                await SendMessageAsync(chatId.Value,
                    "Ты не зарегистрирован. Используй /start для регистрации.",
                    cancellationToken);
                return;
            }

            // Извлекаем название привычки из текста
            // Формат: "Выполнено! [название]" или просто "Выполнено!" для последней привычки
            var habitName = ExtractHabitName(messageText);

            if (string.IsNullOrEmpty(habitName))
            {
                // Если название не указано, предлагаем выбрать из списка
                var habits = await _habitService.GetUserHabitsAsync(user.Id, cancellationToken);

                if (habits == null || !habits.Any())
                {
                    await SendMessageAsync(chatId.Value,
                        "У тебя пока нет привычек. Используй /add чтобы добавить.",
                        cancellationToken);
                    return;
                }

                if (habits.Count() == 1)
                {
                    // Если привычка одна, отмечаем её
                    await MarkHabitCompletedAsync(chatId.Value, habits.First().Id, habits.First().Name, cancellationToken);
                }
                else
                {
                    // Несколько привычек - просим уточнить
                    await SendMessageAsync(chatId.Value,
                        "У тебя несколько привычек. Уточни какую отметить:\n\n" +
                        "Выполнено! [название привычки]",
                        cancellationToken);
                }
                return;
            }

            // Ищем привычку по названию
            var userHabits = await _habitService.GetUserHabitsAsync(user.Id, cancellationToken);
            var habit = userHabits?.FirstOrDefault(h => 
                h.Name.Equals(habitName, StringComparison.OrdinalIgnoreCase));

            if (habit == null)
            {
                await SendMessageAsync(chatId.Value,
                    $"Привычка \"{habitName}\" не найдена. 🤔\n\n" +
                    "Используй /list чтобы посмотреть свои привычки.",
                    cancellationToken);
                return;
            }

            await MarkHabitCompletedAsync(chatId.Value, habit.Id, habit.Name, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка отметки выполнения привычки для пользователя {UserId}", userId);
            await SendMessageAsync(chatId.Value,
                "Произошла ошибка при отметке выполнения.",
                cancellationToken);
        }
    }

    private async Task MarkHabitCompletedAsync(long chatId, long habitId, string habitName, CancellationToken cancellationToken)
    {
        try
        {
            var createLogDto = new CreateHabitLogDto
            {
                HabitId = habitId,
                CompletedAt = DateTime.UtcNow
            };

            await _habitLogService.CreateLogAsync(createLogDto, cancellationToken);

            await SendMessageAsync(chatId,
                $"Отлично! 🎉 Привычка \"{habitName}\" отмечена как выполненная.\n\n" +
                "Так держать! 💪",
                cancellationToken);

            Logger.LogInformation("Отмечена привычка {HabitId} для пользователя в чате {ChatId}", habitId, chatId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка создания записи о выполнении привычки {HabitId}", habitId);
            throw;
        }
    }

    private static string? ExtractHabitName(string messageText)
    {
        // Убираем "Выполнено!" и берём оставшийся текст
        var text = messageText.Replace("Выполнено!", "", StringComparison.OrdinalIgnoreCase)
                              .Replace("выполнено!", "", StringComparison.OrdinalIgnoreCase)
                              .Trim();

        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}
