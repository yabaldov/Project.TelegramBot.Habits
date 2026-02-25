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
            var dailySummaryJobKey = new JobKey("DailySummaryJob");
            q.AddJob<DailySummaryJob>(opts => opts.WithIdentity(dailySummaryJobKey));
            q.AddTrigger(opts => opts
                .ForJob(dailySummaryJobKey)
                .WithIdentity("DailySummaryJob-trigger")
                .WithCronSchedule("0 * * ? * *")); // Каждую минуту (на 0-й секунде)

            // Регистрация ReminderJob — запускается каждую минуту для отправки напоминаний о привычках
            var reminderJobKey = new JobKey("ReminderJob");
            q.AddJob<ReminderJob>(opts => opts.WithIdentity(reminderJobKey));
            q.AddTrigger(opts => opts
                .ForJob(reminderJobKey)
                .WithIdentity("ReminderJob-trigger")
                .WithCronSchedule("0 * * ? * *")); // Каждую минуту (на 0-й секунде)
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        return services;
    }
}
