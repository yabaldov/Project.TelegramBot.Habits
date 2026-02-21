using HabiBot.Application.Services;
using HabiBot.Bot.StateManagement;
using HabiBot.Infrastructure.Models.Telegram;
using HabiBot.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace HabiBot.Bot.Commands;

/// <summary>
/// Команда /stats - показать статистику выполнения привычек
/// </summary>
public class StatsCommand : BotCommandBase
{
    private readonly IUserService _userService;
    private readonly IStatisticsService _statisticsService;

    public StatsCommand(
        ITelegramApiClient telegramClient,
        IUserStateManager stateManager,
        IUserService userService,
        IStatisticsService statisticsService,
        ILogger<StatsCommand> logger)
        : base(telegramClient, stateManager, logger)
    {
        _userService = userService;
        _statisticsService = statisticsService;
    }

    public override string Name => "stats";
    public override string Description => "Показать статистику выполнения";

    public override async Task ExecuteAsync(Update update, CancellationToken cancellationToken = default)
    {
        var chatId = GetChatId(update);
        var userId = GetUserId(update);
        var messageText = GetMessageText(update);

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

            // Определяем период (по умолчанию - неделя)
            var period = ExtractPeriod(messageText);
            var stats = await GetStatisticsAsync(user.Id, period, cancellationToken);

            var message = FormatStatistics(stats, period);
            await SendMessageAsync(chatId.Value, message, cancellationToken);

            Logger.LogInformation("Показана статистика {Period} для пользователя {UserId}", period, userId.Value);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Ошибка получения статистики для пользователя {UserId}", userId);
            await SendMessageAsync(chatId.Value,
                "Произошла ошибка при получении статистики.",
                cancellationToken);
        }
    }

    private async Task<Application.DTOs.UserStatistics> GetStatisticsAsync(long userId, StatsPeriod period, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        return period switch
        {
            StatsPeriod.Today => await _statisticsService.GetDailyStatisticsAsync(userId, now, cancellationToken),
            StatsPeriod.Week => await _statisticsService.GetWeeklyStatisticsAsync(userId, now.AddDays(-7), cancellationToken),
            StatsPeriod.Month => await _statisticsService.GetMonthlyStatisticsAsync(userId, now.Year, now.Month, cancellationToken),
            _ => await _statisticsService.GetWeeklyStatisticsAsync(userId, now.AddDays(-7), cancellationToken)
        };
    }

    private string FormatStatistics(Application.DTOs.UserStatistics stats, StatsPeriod period)
    {
        var sb = new StringBuilder();
        var periodName = period switch
        {
            StatsPeriod.Today => "сегодня",
            StatsPeriod.Week => "за неделю",
            StatsPeriod.Month => "за месяц",
            _ => "за неделю"
        };

        sb.AppendLine($"📊 **Статистика {periodName}**");
        sb.AppendLine();
        sb.AppendLine($"📅 Период: {stats.PeriodStart:dd.MM.yyyy} - {stats.PeriodEnd:dd.MM.yyyy}");
        sb.AppendLine($"🎯 Привычек: {stats.TotalHabits}");
        sb.AppendLine($"✅ Всего выполнений: {stats.TotalCompletions}");
        
        if (period != StatsPeriod.Today)
        {
            sb.AppendLine($"⭐ Сегодня выполнено: {stats.CompletionsToday}");
        }

        sb.AppendLine();

        if (!stats.HabitStats.Any())
        {
            sb.AppendLine("У тебя пока нет привычек.");
            sb.AppendLine("Используй /add чтобы добавить первую!");
            return sb.ToString();
        }

        sb.AppendLine("**Детальная статистика:**");
        sb.AppendLine();

        foreach (var habitStat in stats.HabitStats.OrderByDescending(h => h.CompletionRate))
        {
            sb.AppendLine($"📌 **{habitStat.HabitName}**");
            sb.AppendLine($"   Выполнено: {habitStat.CompletedCount}/{habitStat.ExpectedCount} ({habitStat.CompletionRate:F0}%)");

            if (habitStat.CurrentStreak > 0)
            {
                sb.AppendLine($"   🔥 Текущая серия: {habitStat.CurrentStreak} {GetDaysWord(habitStat.CurrentStreak)}");
            }

            if (habitStat.BestStreak > 0)
            {
                sb.AppendLine($"   🏆 Лучшая серия: {habitStat.BestStreak} {GetDaysWord(habitStat.BestStreak)}");
            }

            if (habitStat.LastCompleted.HasValue)
            {
                var lastCompletedLocal = habitStat.LastCompleted.Value;
                sb.AppendLine($"   📅 Последнее выполнение: {lastCompletedLocal:dd.MM.yyyy HH:mm}");
            }

            sb.AppendLine();
        }

        sb.AppendLine("Для просмотра другого периода:");
        sb.AppendLine("/stats today - сегодня");
        sb.AppendLine("/stats week - неделя");
        sb.AppendLine("/stats month - месяц");

        return sb.ToString();
    }

    private StatsPeriod ExtractPeriod(string? messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText))
            return StatsPeriod.Week;

        var text = messageText.ToLowerInvariant();

        if (text.Contains("today") || text.Contains("сегодня"))
            return StatsPeriod.Today;

        if (text.Contains("month") || text.Contains("месяц"))
            return StatsPeriod.Month;

        return StatsPeriod.Week;
    }

    private string GetDaysWord(int days)
    {
        var lastDigit = days % 10;
        var lastTwoDigits = days % 100;

        if (lastTwoDigits >= 11 && lastTwoDigits <= 19)
            return "дней";

        return lastDigit switch
        {
            1 => "день",
            2 or 3 or 4 => "дня",
            _ => "дней"
        };
    }

    private enum StatsPeriod
    {
        Today,
        Week,
        Month
    }
}
