using HabiBot.Application;
using HabiBot.Bot.Commands;
using HabiBot.Bot.Services;
using HabiBot.Bot.StateManagement;
using HabiBot.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

// Предварительная настройка Serilog для логирования до запуска хоста
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Запуск HabiBot");

    var builder = Host.CreateApplicationBuilder(args);

    // Настройка конфигурации
    builder.Configuration
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
        .AddUserSecrets<Program>()
        .AddEnvironmentVariables();

    // Настройка Serilog из конфигурации
    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.File(
            path: "logs/habibot-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"));

    // Регистрация слоёв приложения
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplication();

    // Регистрация Rate Limiting
    builder.Services.Configure<HabiBot.Bot.Middleware.RateLimitingOptions>(
        builder.Configuration.GetSection(HabiBot.Bot.Middleware.RateLimitingOptions.SectionName));
    builder.Services.AddSingleton<HabiBot.Bot.Middleware.RateLimitingService>();

    // Регистрация State Management
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<IUserStateManager, UserStateManager>();

// Регистрация команд
builder.Services.AddScoped<StartCommand>();
builder.Services.AddScoped<HelpCommand>();
builder.Services.AddScoped<ListCommand>();
builder.Services.AddScoped<AddCommand>();
builder.Services.AddScoped<StatsCommand>();
builder.Services.AddScoped<EditCommand>();
builder.Services.AddScoped<DeleteCommand>();
builder.Services.AddScoped<CompletedHandler>();

// Регистрация сервисов бота
builder.Services.AddSingleton<CommandRouter>();
builder.Services.AddSingleton<BotUpdateHandler>();
builder.Services.AddHostedService<BotBackgroundService>();

var host = builder.Build();

// Запуск приложения
await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Приложение завершилось с критической ошибкой");
    return 1;
}
finally
{
    Log.Information("Остановка HabiBot");
    await Log.CloseAndFlushAsync();
}

return 0;
