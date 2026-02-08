# Инструкции для GitHub Copilot

## Описание проекта
Telegram-бот "Трекер привычек HabiBot" для отслеживания и мониторинга привычек пользователей.

## Стек технологий
- .NET 10.0
- System.Net.Http.HttpClient — прямые HTTP-запросы к Telegram Bot API (без сторонних библиотек-обёрток)
- System.Text.Json — сериализация/десериализация JSON
- Entity Framework Core (ORM для работы с БД)
- PostgreSQL (база данных)
- Quartz.NET (планировщик задач для напоминаний)
- Serilog (логирование)
- FluentValidation (валидация входных данных)
- xUnit.net (тестирование)
- Moq (мокирование зависимостей в тестах)
- FluentAssertions (расширенные утверждения в тестах)

## Архитектура и структура проекта

- Clean architecture с разделением на слои.
- Весь код, все проекты находятся в папке `src/`.
- Все тесты в папке `tests/`.

### Слои приложения
- **HabiBot.Domain** — доменные модели (User, Habit, HabitLog)
- **HabiBot.Application** — бизнес-логика, сервисы, интерфейсы
- **HabiBot.Infrastructure** — реализация репозиториев, EF Core, внешние сервисы
- **HabiBot.Bot** — Telegram Bot handlers, команды, обработка сообщений
- **HabiBot.Tests** — unit и integration тесты

### Паттерны проектирования
- Repository Pattern для доступа к данным
- Unit of Work для транзакций
- Dependency Injection (встроенный DI контейнер .NET)
- CQRS для разделения команд и запросов (опционально)
- Strategy Pattern для различных типов напоминаний

## Работа с Telegram Bot API

### Основные правила
- **Не использовать** NuGet-пакет `Telegram.Bot` и любые другие сторонние библиотеки-обёртки для Telegram Bot API.
- Для взаимодействия с Telegram Bot API использовать **только** `System.Net.Http.HttpClient` и прямые HTTP-запросы к `https://api.telegram.org/bot<token>/<method>`.
- Для сериализации/десериализации JSON использовать `System.Text.Json`.
- Создавать собственные DTO-модели (record/class) для запросов и ответов Telegram Bot API по мере необходимости.
- `HttpClient` должен регистрироваться через `IHttpClientFactory` (или как typed/named client) в DI-контейнере, а не создаваться вручную через `new HttpClient()`.

### Структура DTO
- Модели запросов и ответов Telegram API размещать в папке `Models/Telegram/`.
- Использовать атрибуты `[JsonPropertyName("...")]` для маппинга snake_case полей API на PascalCase свойства C#.
- Предпочитать `record` типы для DTO.

### Обработка команд
- Все команды наследуются от базового `IBotCommand`.
- Валидация входных данных через FluentValidation.
- Обработка ошибок через middleware/фильтры.

### State Management
- Используй In-Memory Cache для хранения состояния диалога.
- Реализуй FSM (Finite State Machine) для многошаговых диалогов.
- Timeout для сессий пользователя.

### Напоминания
- Реализация напоминанийв последнюю очередь, после реализации основной функциональности.
- Quartz.NET для scheduled jobs.
- Cron-выражения для настройки времени напоминаний.
- Обработка временных зон пользователей.

## Соглашения о коде

### Именование
- PascalCase для классов, методов, свойств
- camelCase для локальных переменных и параметров
- Интерфейсы начинаются с `I` (IHabitRepository)
- Async методы заканчиваются на `Async`
- Private поля начинаются с `_` (_logger, _repository)

### Стиль кода
- Использовать `async/await` для всех асинхронных операций
- Nullable Reference Types включены
- File-scoped namespaces (C# 10+)
- Record types для DTO и value objects
- Pattern matching где возможно
- Минимальный API для простых эндпоинтов

### Документация
- XML-комментарии для публичных API
- Summary для всех публичных классов и методов
- Remarks для важных деталей реализации

## База данных

### Модели
- Всегда используй `Id` как первичный ключ (long для Telegram User ID)
- DateTime в UTC
- Мягкое удаление через `IsDeleted` флаг
- Audit поля: CreatedAt, UpdatedAt, CreatedBy, UpdatedBy

### Миграции
- Описательные имена миграций
- Разделение data migrations и schema migrations
- Seed data для справочников

## Логирование и мониторинг
- Structured logging через Serilog
- Log levels: Debug для разработки, Information для production
- Логировать все исключения с контекстом
- Использовать correlation ID для трейсинга запросов

## Тестирование
- Unit тесты для бизнес-логики
- Integration тесты для репозиториев и внешних сервисов
- Arrange-Act-Assert паттерн
- Mock внешние зависимости через Moq
- Test fixtures для переиспользования setup кода

## Конфигурация
- appsettings.json для настроек
- User Secrets для чувствительных данных в dev
- Environment Variables для production
- Strongly typed configuration через IOptions<T>

## Безопасность
- Не хранить токены и пароли в коде
- Валидация всех пользовательских входных данных
- Rate limiting для предотвращения спама
- Sanitize данных перед отправкой в Telegram

## Примеры

### Отправка сообщения через Telegram Bot API
```csharp
var payload = new SendMessageRequest { ChatId = chatId, Text = message };
var content = new StringContent(
    JsonSerializer.Serialize(payload),
    Encoding.UTF8,
    "application/json");

var response = await httpClient.PostAsync(
    $"https://api.telegram.org/bot{botToken}/sendMessage", content);

response.EnsureSuccessStatusCode();
```

### Команда бота
```csharp
public class AddHabitCommand : IBotCommand
{
    private readonly IHabitService _habitService;

    public async Task ExecuteAsync(Update update, CancellationToken ct)
    {
        // Implementation
    }
}
```

### Репозиторий
```csharp
public interface IHabitRepository : IRepository<Habit>
{
    Task<IEnumerable<Habit>> GetUserHabitsAsync(long userId, CancellationToken ct);
}
```

### Сервис
```csharp
public class HabitService : IHabitService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HabitService> _logger;

    public async Task<Result<Habit>> CreateHabitAsync(CreateHabitDto dto, CancellationToken ct)
    {
        // Implementation with validation and error handling
    }
}
```