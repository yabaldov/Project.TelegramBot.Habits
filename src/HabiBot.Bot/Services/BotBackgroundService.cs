using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HabiBot.Bot.Services;

/// <summary>
/// Background Service для запуска бота
/// </summary>
public class BotBackgroundService : BackgroundService
{
    private readonly BotUpdateHandler _updateHandler;
    private readonly ILogger<BotBackgroundService> _logger;

    public BotBackgroundService(
        BotUpdateHandler updateHandler,
        ILogger<BotBackgroundService> logger)
    {
        _updateHandler = updateHandler;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Бот запускается...");

        try
        {
            await _updateHandler.StartPollingAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Критическая ошибка в работе бота");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Бот останавливается...");
        await base.StopAsync(cancellationToken);
    }
}
