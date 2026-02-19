using FluentValidation;
using HabiBot.Application.Services;
using HabiBot.Application.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace HabiBot.Application;

/// <summary>
/// Методы расширения для регистрации Application зависимостей
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Добавить Application сервисы
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Регистрация валидаторов из текущей сборки
        services.AddValidatorsFromAssemblyContaining<CreateUserDtoValidator>();

        // Регистрация сервисов
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IHabitService, HabitService>();
        services.AddScoped<IHabitLogService, HabitLogService>();
        services.AddScoped<IStatisticsService, StatisticsService>();

        return services;
    }
}
