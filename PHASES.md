# 📋 Поэтапный план реализации HabiBot

## 🎯 Фаза 1: Структура решения и базовая настройка ✅
- [x] Создать solution (.sln) и структуру папок `src/` и `tests/`
- [x] Создать проекты по слоям Clean Architecture:
  - `src/HabiBot.Domain` (class library)
  - `src/HabiBot.Application` (class library)
  - `src/HabiBot.Infrastructure` (class library)
  - `src/HabiBot.Bot` (console/web app - точка входа)
  - `tests/HabiBot.Tests` (xUnit test project)
- [x] Настроить зависимости между проектами
- [x] Добавить `.gitignore` и `Directory.Build.props`
- [x] Настроить Nullable Reference Types и file-scoped namespaces

## 🏗️ Фаза 2: Domain Layer (доменная модель) ✅
- [x] Создать базовые сущности:
  - `User` (Id: long для Telegram, Name, TimeZone, CreatedAt/UpdatedAt)
  - `Habit` (Name, ReminderTime, Frequency)
  - `HabitLog` (отметки выполнения)
- [x] Добавить Audit поля (CreatedAt, UpdatedAt, IsDeleted)
- [x] Создать value objects и enums (HabitFrequency)
- [x] Определить интерфейсы репозиториев (IRepository<T>, IUnitOfWork)

## 🔧 Фаза 3: Infrastructure - База данных ✅
- [x] Установить NuGet: EF Core, Npgsql.EntityFrameworkCore.PostgreSQL
- [x] Создать `HabiBotDbContext` с конфигурациями сущностей
- [x] Настроить подключение к PostgreSQL через appsettings.json
- [x] Реализовать Repository Pattern и Unit of Work
- [x] Создать первую миграцию БД (`Initial`)
- [x] Настроить User Secrets для строки подключения (dev)

## 📡 Фаза 4: Telegram API Integration (БЕЗ библиотек) ✅
- [x] Создать папку `Models/Telegram/` в Infrastructure
- [x] Создать собственные DTO (record types) с `[JsonPropertyName]`:
  - `Update`, `Message`, `Chat`, `TelegramUser`
  - `SendMessageRequest`, `TelegramResponse`, `GetUpdatesRequest`
- [x] Реализовать `ITelegramApiClient` интерфейс
- [x] Реализовать `TelegramApiClient` используя **только HttpClient**
- [x] Зарегистрировать HttpClient через `IHttpClientFactory` в DI
- [x] Настроить Long Polling (`getUpdates`) для получения сообщений
- [x] Сохранить Bot Token в User Secrets

## 💼 Фаза 5: Application Layer - Сервисы ✅
- [x] Установить NuGet: FluentValidation, FluentValidation.DependencyInjectionExtensions
- [x] Создать интерфейсы сервисов:
  - `IUserService` (регистрация, поиск)
  - `IHabitService` (CRUD привычек)
  - `IHabitLogService` (отметки выполнения)
- [x] Реализовать сервисы с валидацией
- [x] Создать DTO для команд (CreateHabitDto, etc.)
- [x] Добавить валидаторы FluentValidation

## 🤖 Фаза 6: Bot - State Management и команды ✅
- [x] Создать FSM (Finite State Machine) для диалогов
- [x] Реализовать In-Memory Cache для хранения состояния пользователя
- [x] Создать интерфейс `IBotCommand` с методом `ExecuteAsync`
- [x] Реализовать базовые команды MVP:
  - `/start` - регистрация пользователя (многошаговый диалог: спросить имя)
  - `/add` - добавление привычки (многошаговый: название → время)
  - `/list` - список привычек пользователя
  - Обработка текста "Выполнено!" для отметки привычки
  - `/help` - справка по командам
- [x] Создать Command Router для маршрутизации команд
- [x] Настроить обработку ошибок и логирование
- [x] Обработка неизвестных команд с подсказкой пользователю

## 📊 Фаза 7: Статистика и отчеты ✅
- [x] Реализовать `IStatisticsService`
- [x] Реализовать команду `/stats` с выбором периода
- [x] Генерация отчетов:
  - За день (`/stats today`)
  - За неделю (`/stats week`)
  - За месяц (`/stats month`)
  - Произвольный период (from, to)
- [x] Расчет текущей и лучшей серии (streak)
- [x] Процент выполнения привычек
- [x] Форматирование текста для красивого вывода в Telegram с русской плюрализацией

## ✏️ Фаза 8: Редактирование и удаление ✅
- [x] Команда `/edit` - редактирование привычки (название, время, частота)
- [x] Команда `/delete` - удаление привычки (мягкое удаление)
- [x] Inline клавиатуры для выбора привычки

## 🔒 Фаза 9: Безопасность и производительность ✅
- [x] Реализовать Rate Limiting (защита от спама)
- [x] Sanitize пользовательских данных перед отправкой
- [x] Настроить Serilog с structured logging
- [x] Настроить разные log levels (Debug/Information)
- [x] Логирование в файлы с ротацией

## 🧪 Фаза 10: Тестирование ✅
- [x] Установить NuGet: xUnit, Moq, FluentAssertions, EF Core InMemory
- [x] Unit тесты для сервисов (HabitService, UserService, StatisticsService)
- [x] Unit тесты для команд бота (StartCommand, EditCommand, DeleteCommand)
- [x] Unit тесты для Domain (User, Habit, HabitLog)
- [x] Тестирование валидаторов
- [x] Integration тесты для репозиториев (InMemory DB) - 10 тестов
- [x] Integration тесты для сервисов - 8 тестов
- [x] Всего 58 тестов - все проходят ✅

## 🚀 Фаза 11: MVP Release ✅
- [x] Финальное тестирование всех команд
- [x] Обновить README с инструкциями по запуску
- [x] Документация API (XML комментарии на русском)
- [x] Создан CHANGELOG.md
- [x] Проверка Release сборки и тестов
- [ ] Подготовка к деплою (опционально - не требуется)

---

## 🌟 Фаза 12+ (Опциональные фичи - после MVP)

### 📈 Ежедневная сводка
- [x] Установить NuGet: Quartz, Quartz.Extensions.Hosting (если еще не установлен)
- [x] Добавить поле `DailySummaryTime` (nullable TimeSpan) и `IsDailySummaryEnabled` (bool) к сущности User
- [x] Создать миграцию для добавления полей сводки
- [x] Реализовать `IDailySummaryService` для генерации сводки:
  - Получение статистики по выполненным привычкам за день
  - Список невыполненных привычек на сегодня
  - Процент выполнения от запланированных привычек
  - Список привычек на завтра
- [x] Реализовать команду `/setsummary` для настройки времени и включения/отключения сводки
- [x] Реализовать команду `/summary` для получения сводки в любое время
- [x] Создать Quartz Job для отправки ежедневной сводки:
  - Динамические триггеры по времени каждого пользователя (с учетом временной зоны)
  - Обработка включения/отключения сводки
- [x] Добавить уведомление после добавления/изменения привычки о настройке сводки
- [x] Тестирование сводки и Job

### ⏰ Напоминания (Quartz.NET)
- [ ] Установить NuGet: Quartz, Quartz.Extensions.Hosting
- [ ] Настроить Quartz.NET в DI контейнере
- [ ] Создать Job для отправки напоминаний
- [ ] Настроить динамические триггеры по времени пользователя
- [ ] Обработка временных зон (TimeZoneInfo)
- [ ] Тестирование напоминаний

### 👥 Роль "Бадди"
- [ ] Подписка на ежедневный или еженедельный дайджест друзей
- [ ] Разрешение другим подписываться на свои дайджесты
- [ ] Комментирование дайджестов бадди
- [ ] Управление подписками и разрешениями

### 🛡️ Роль "Администратор"
- [ ] Управление списком пользователей
- [ ] Модерация
- [ ] Настройка глобальных параметров бота

### 🎨 Дополнительные фичи
- [ ] Множественные напоминания в день
- [ ] Шаблоны привычек с предустановками
- [ ] Мотивационные сообщения
- [ ] Экспорт статистики
- [ ] Webhook вместо Long Polling
- [ ] Откладывание привычек
- [ ] Напоминания о приёме лекарств/питании

---

**Начинаем с Фазы 1-3 для создания фундамента проекта**
