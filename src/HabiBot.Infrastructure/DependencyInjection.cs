using HabiBot.Domain.Interfaces;
using HabiBot.Infrastructure.Data;
using HabiBot.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        return services;
    }
}
