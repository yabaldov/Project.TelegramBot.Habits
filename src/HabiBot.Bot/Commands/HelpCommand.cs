using HabiBot.Bot.StateManagement;
using HabiBot.Infrastructure.Models.Telegram;
using HabiBot.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HabiBot.Bot.Commands;

/// <summary>
/// Команда /help - справка по доступным командам
/// </summary>
public class HelpCommand : BotCommandBase
{
    public HelpCommand(
        ITelegramApiClient telegramClient,
        IUserStateManager stateManager,
        ILogger<HelpCommand> logger)
        : base(telegramClient, stateManager, logger)
    {
    }

    public override string Name => "help";
    public override string Description => "Показать справку по командам";

    public override async Task ExecuteAsync(Update update, CancellationToken cancellationToken = default)
    {
        var chatId = GetChatId(update);

        if (chatId == null)
        {
            Logger.LogWarning("Получен Update без ChatId");
            return;
        }

        try
        {
            var helpMessage = @"📚 *Справка по командам HabiBot*

*Основные команды:*
/start \- Регистрация в боте
/help \- Показать эту справку

*Управление привычками:*
/add \- Добавить новую привычку
/list \- Показать все мои привычки
/edit \- Редактировать привычку
/delete \- Удалить привычку

*Статистика и сводка:*
/stats \- Статистика за неделю
/stats today \- Статистика за сегодня
/stats week \- Статистика за неделю
/stats month \- Статистика за месяц
/summary \- Ежедневная сводка за сегодня
/setsummary \- Настроить ежедневную сводку

*Отметка выполнения:*
Выполнено\! \[название\] \- Отметить привычку как выполненную

*Примеры:*
• Выполнено\! Медитация
• Выполнено\! \- если привычка одна";

            await SendMarkdownMessageAsync(chatId.Value, helpMessage, cancellationToken);
            Logger.LogInformation("Показана справка пользователю {ChatId}", chatId.Value);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка выполнения команды /help для чата {ChatId}", chatId);
            await SendMessageAsync(chatId.Value,
                "Произошла ошибка при получении справки.",
                cancellationToken);
        }
    }
}
