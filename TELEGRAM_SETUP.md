# Telegram API - Быстрый старт

## Настройка токена бота

### Вариант 1: User Secrets (для разработки)

```bash
cd src/HabiBot.Bot
dotnet user-secrets set "TelegramBot:BotToken" "YOUR_ACTUAL_BOT_TOKEN"
```

### Вариант 2: Переменные окружения (для production)

```bash
export TelegramBot__BotToken="YOUR_ACTUAL_BOT_TOKEN"
```

### Вариант 3: appsettings.json (не рекомендуется для безопасности)

Отредактируйте `src/HabiBot.Bot/appsettings.json`:

```json
{
  "TelegramBot": {
    "BotToken": "YOUR_ACTUAL_BOT_TOKEN"
  }
}
```

## Получение токена бота

1. Откройте Telegram и найдите @BotFather
2. Отправьте команду `/newbot`
3. Следуйте инструкциям для создания бота
4. Скопируйте токен, который выдаст BotFather
5. Используйте его в настройках выше

## Проверка интеграции

Создайте простой тест в `Program.cs`:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);

var host = builder.Build();

var client = host.Services.GetRequiredService<ITelegramApiClient>();
var botInfo = await client.GetMeAsync();
Console.WriteLine($"Бот успешно подключен: @{botInfo.Username}");
```
