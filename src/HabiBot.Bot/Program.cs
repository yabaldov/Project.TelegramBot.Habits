using HabiBot.Application;
using HabiBot.Bot.Commands;
using HabiBot.Bot.Services;
using HabiBot.Bot.StateManagement;
using HabiBot.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Настройка конфигурации
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables();

// Настройка логирования
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

// Регистрация слоёв приложения
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// Регистрация State Management
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IUserStateManager, UserStateManager>();

// Регистрация команд
builder.Services.AddScoped<StartCommand>();
builder.Services.AddScoped<HelpCommand>();
builder.Services.AddScoped<ListCommand>();
builder.Services.AddScoped<AddCommand>();
builder.Services.AddScoped<CompletedHandler>();

// Регистрация сервисов бота
builder.Services.AddSingleton<CommandRouter>();
builder.Services.AddSingleton<BotUpdateHandler>();
builder.Services.AddHostedService<BotBackgroundService>();

var host = builder.Build();

// Запуск приложения
await host.RunAsync();
