using HabiBot.Application.Services;
using HabiBot.Bot.StateManagement;
using HabiBot.Infrastructure.Models.Telegram;
using HabiBot.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace HabiBot.Bot.Commands;

/// <summary>
/// Команда /list - показать список привычек пользователя
/// </summary>
public class ListCommand : BotCommandBase
{
    private readonly IUserService _userService;
    private readonly IHabitService _habitService;

    public ListCommand(
        ITelegramApiClient telegramClient,
        IUserStateManager stateManager,
        IUserService userService,
        IHabitService habitService,
        ILogger<ListCommand> logger)
        : base(telegramClient, stateManager, logger)
    {
        _userService = userService;
        _habitService = habitService;
    }

    public override string Name => "list";
    public override string Description => "Показать список привычек";

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
            // Проверяем регистрацию
            var user = await _userService.GetByTelegramIdAsync(userId.Value, cancellationToken);
            
            if (user == null)
            {
                await SendMessageAsync(chatId.Value, 
                    "Ты не зарегистрирован. Используй /start для регистрации.", 
                    cancellationToken,
                    replyMarkup: ReplyKeyboardHelper.PreRegistrationKeyboard);
                return;
            }

            // Получаем привычки пользователя
            var habits = await _habitService.GetUserHabitsAsync(user.Id, cancellationToken);

            if (habits == null || !habits.Any())
            {
                await SendMessageAsync(chatId.Value, 
                    "У тебя пока нет привычек. 📝\n\n" +
                    "Используй /add чтобы добавить первую привычку!", 
                    cancellationToken);
                return;
            }

            // Форматируем список привычек
            var sb = new StringBuilder();
            sb.AppendLine("Твои привычки: 📋\n");

            foreach (var habit in habits.OrderBy(h => h.ReminderTime))
            {
                var frequency = habit.Frequency switch
                {
                    Domain.Enums.HabitFrequency.Daily => "Ежедневно",
                    Domain.Enums.HabitFrequency.Weekly => "Еженедельно",
                    _ => "По расписанию"
                };

                sb.AppendLine($"• `{habit.Name}`");
                sb.AppendLine($"  ⏰ {habit.ReminderTime} \\({frequency}\\)");
                sb.AppendLine();
            }

            sb.AppendLine("Чтобы отметить выполнение, напиши: Выполнено\\! \\[название привычки\\]");

            await SendMarkdownMessageAsync(chatId.Value, sb.ToString(), cancellationToken);
            Logger.LogInformation("Показан список из {Count} привычек для пользователя {UserId}", habits.Count(), userId.Value);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка получения списка привычек для пользователя {UserId}", userId);
            await SendMessageAsync(chatId.Value, 
                "Произошла ошибка при получении списка привычек.", 
                cancellationToken);
        }
    }
}
