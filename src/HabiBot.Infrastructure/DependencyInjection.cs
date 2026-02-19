using HabiBot.Domain.Interfaces;
using HabiBot.Infrastructure.Data;
using HabiBot.Infrastructure.Jobs;
using HabiBot.Infrastructure.Repositories;
using HabiBot.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace HabiBot.Infrastructure;

/// <summary>
/// Методы расширения для регистрации Infrastructure зависимостей
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Добавить Infrastructure сервисы
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Регистрация DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<HabiBotDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Регистрация репозиториев
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IHabitRepository, HabitRepository>();
        services.AddScoped<IHabitLogRepository, HabitLogRepository>();

        // Регистрация Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Регистрация настроек Telegram бота
        services.Configure<TelegramBotOptions>(
            configuration.GetSection(TelegramBotOptions.SectionName));

        // Регистрация HttpClient для Telegram API
        services.AddHttpClient<ITelegramApiClient, TelegramApiClient>();

        // Регистрация Quartz.NET для планирования задач
        services.AddQuartz(q =>
        {
            // Регистрация DailySummaryJob — запускается каждую минуту
            var jobKey = new JobKey("DailySummaryJob");
            q.AddJob<DailySummaryJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("DailySummaryJob-trigger")
                .WithCronSchedule("0 * * ? * *")); // Каждую минуту (на 0-й секунде)
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        return services;
    }
}
