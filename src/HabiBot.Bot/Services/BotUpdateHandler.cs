using HabiBot.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace HabiBot.Bot.Services;

/// <summary>
/// Сервис для получения и обработки обновлений от Telegram
/// </summary>
public class BotUpdateHandler
{
    private readonly ITelegramApiClient _telegramClient;
    private readonly CommandRouter _commandRouter;
    private readonly ILogger<BotUpdateHandler> _logger;
    private long _lastUpdateId = 0;

    public BotUpdateHandler(
        ITelegramApiClient telegramClient,
        CommandRouter commandRouter,
        ILogger<BotUpdateHandler> logger)
    {
        _telegramClient = telegramClient;
        _commandRouter = commandRouter;
        _logger = logger;
    }

    /// <summary>
    /// Запустить обработку обновлений (Long Polling)
    /// </summary>
    public async Task StartPollingAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запуск Long Polling...");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var request = new Infrastructure.Models.Telegram.GetUpdatesRequest
                {
                    Offset = _lastUpdateId + 1
                };
                
                var updates = await _telegramClient.GetUpdatesAsync(request, cancellationToken);

                if (updates != null && updates.Any())
                {
                    _logger.LogDebug("Получено {Count} обновлений", updates.Count());

                    foreach (var update in updates)
                    {
                        try
                        {
                            // Обрабатываем обновление
                            await _commandRouter.RouteAsync(update, cancellationToken);

                            // Обновляем последний ID
                            if (update.UpdateId > _lastUpdateId)
                            {
                                _lastUpdateId = update.UpdateId;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Ошибка обработки обновления {UpdateId}", update.UpdateId);
                        }
                    }
                }

                // Небольшая задержка между запросами если нет обновлений
                if (updates == null || !updates.Any())
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Long Polling остановлен");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении обновлений");
                
                // Задержка перед повтором при ошибке
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }
}
