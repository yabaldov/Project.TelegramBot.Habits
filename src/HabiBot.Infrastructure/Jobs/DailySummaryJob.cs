using HabiBot.Application.Services;
using HabiBot.Domain.Interfaces;
using HabiBot.Infrastructure.Models.Telegram;
using HabiBot.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace HabiBot.Infrastructure.Jobs;

/// <summary>
/// Quartz Job для отправки ежедневных сводок пользователям.
/// Запускается каждую минуту и проверяет, каким пользователям пора отправить сводку.
/// </summary>
[DisallowConcurrentExecution]
public class DailySummaryJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailySummaryJob> _logger;

    public DailySummaryJob(
        IServiceScopeFactory scopeFactory,
        ILogger<DailySummaryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogDebug("Запуск DailySummaryJob");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var dailySummaryService = scope.ServiceProvider.GetRequiredService<IDailySummaryService>();
            var telegramClient = scope.ServiceProvider.GetRequiredService<ITelegramApiClient>();

            // Получаем всех пользователей с включённой сводкой
            var users = await unitOfWork.Users.GetUsersWithDailySummaryEnabledAsync(context.CancellationToken);
            var usersList = users.ToList();

            if (!usersList.Any())
            {
                _logger.LogDebug("Нет пользователей с включённой сводкой");
                return;
            }

            var now = DateTime.UtcNow;
            var sentCount = 0;

            foreach (var user in usersList)
            {
                try
                {
                    if (!ShouldSendSummaryNow(user, now))
                    {
                        continue;
                    }

                    _logger.LogInformation("Отправка сводки пользователю {UserId} ({TelegramId})",
                        user.Id, user.TelegramUserId);

                    // Определяем дату для сводки в часовом поясе пользователя
                    var userDate = GetUserDate(user, now);

                    // Получаем данные сводки для генерации inline keyboard
                    var summaryData = await dailySummaryService.GetDailySummaryAsync(
                        user.Id, userDate, context.CancellationToken);

                    // Генерируем текст сводки (с планом на следующий день — это плановая сводка)
                    var summaryText = await dailySummaryService.GenerateSummaryTextAsync(
                        user.Id,
                        userDate,
                        includeNextDay: true,
                        context.CancellationToken);

                    // Генерируем inline keyboard для невыполненных привычек
                    var keyboard = Services.SummaryKeyboardBuilder.BuildUncompletedHabitsKeyboard(summaryData);

                    // Отправляем сводку
                    var request = new SendMessageRequest
                    {
                        ChatId = user.TelegramChatId,
                        Text = summaryText,
                        ParseMode = "HTML",
                        ReplyMarkup = keyboard
                    };
                    await telegramClient.SendMessageAsync(request, context.CancellationToken);

                    sentCount++;
                    _logger.LogInformation("Сводка отправлена пользователю {TelegramId}", user.TelegramUserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка отправки сводки пользователю {TelegramId}", user.TelegramUserId);
                }
            }

            _logger.LogInformation("DailySummaryJob завершён. Отправлено сводок: {Count}", sentCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Критическая ошибка в DailySummaryJob");
        }
    }

    /// <summary>
    /// Проверяет, нужно ли отправлять сводку пользователю в текущий момент.
    /// Job запускается каждую минуту, поэтому проверяем совпадение часа и минуты
    /// с учётом временной зоны пользователя.
    /// </summary>
    private bool ShouldSendSummaryNow(Domain.Entities.User user, DateTime utcNow)
    {
        if (!user.IsDailySummaryEnabled || user.DailySummaryTime == null)
        {
            return false;
        }

        var userNow = GetUserDateTime(user, utcNow);
        var summaryTime = user.DailySummaryTime.Value;

        // Проверяем совпадение часа и минуты
        return userNow.Hour == summaryTime.Hours && userNow.Minute == summaryTime.Minutes;
    }

    /// <summary>
    /// Получает текущее время пользователя с учётом его временной зоны
    /// </summary>
    private DateTime GetUserDateTime(Domain.Entities.User user, DateTime utcNow)
    {
        if (string.IsNullOrEmpty(user.TimeZone))
        {
            return utcNow;
        }

        try
        {
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZone);
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZoneInfo);
        }
        catch (TimeZoneNotFoundException)
        {
            _logger.LogWarning("Часовой пояс {TimeZone} не найден для пользователя {UserId}",
                user.TimeZone, user.Id);
            return utcNow;
        }
    }

    /// <summary>
    /// Получает дату для сводки в часовом поясе пользователя
    /// </summary>
    private DateOnly GetUserDate(Domain.Entities.User user, DateTime utcNow)
    {
        var userDateTime = GetUserDateTime(user, utcNow);
        return DateOnly.FromDateTime(userDateTime);
    }
}
