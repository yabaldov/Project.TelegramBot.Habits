using HabiBot.Domain.Entities;
using HabiBot.Infrastructure.Data;
using HabiBot.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;

namespace HabiBot.Tests.Integration;

/// <summary>
/// Integration тесты для репозиториев с InMemory БД
/// </summary>
public class RepositoryIntegrationTests : IDisposable
{
    private readonly HabiBotDbContext _context;
    private readonly UserRepository _userRepository;
    private readonly HabitRepository _habitRepository;
    private readonly HabitLogRepository _habitLogRepository;

    public RepositoryIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<HabiBotDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new HabiBotDbContext(options);
        _userRepository = new UserRepository(_context);
        _habitRepository = new HabitRepository(_context);
        _habitLogRepository = new HabitLogRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task UserRepository_AddAndGet_WorksCorrectly()
    {
        // Arrange
        var user = new User
        {
            TelegramUserId = 123456,
            TelegramChatId = 123456,
            Name = "Тестовый Пользователь",
            TelegramFirstName = "Тест",
            RegisteredAt = DateTime.UtcNow
        };

        // Act
        await _userRepository.AddAsync(user);
        await _context.SaveChangesAsync();

        var retrieved = await _userRepository.GetByIdAsync(user.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Тестовый Пользователь");
        retrieved.TelegramUserId.Should().Be(123456);
    }

    [Fact]
    public async Task UserRepository_GetByTelegramId_WorksCorrectly()
    {
        // Arrange
        var user = new User
        {
            TelegramUserId = 999888,
            TelegramChatId = 999888,
            Name = "Test User",
            RegisteredAt = DateTime.UtcNow
        };
        await _userRepository.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userRepository.GetByTelegramIdAsync(999888);

        // Assert
        result.Should().NotBeNull();
        result!.TelegramUserId.Should().Be(999888);
    }

    [Fact]
    public async Task HabitRepository_AddAndGetUserHabits_WorksCorrectly()
    {
        // Arrange
        var user = new User
        {
            TelegramUserId = 111222,
            TelegramChatId = 111222,
            Name = "Habit Tester",
            RegisteredAt = DateTime.UtcNow
        };
        await _userRepository.AddAsync(user);
        await _context.SaveChangesAsync();

        var habit1 = new Habit
        {
            UserId = user.Id,
            Name = "Медитация",
            ReminderTime = "09:00"
        };
        var habit2 = new Habit
        {
            UserId = user.Id,
            Name = "Зарядка",
            ReminderTime = "07:00"
        };

        await _habitRepository.AddAsync(habit1);
        await _habitRepository.AddAsync(habit2);
        await _context.SaveChangesAsync();

        // Act
        var habits = await _habitRepository.GetUserHabitsAsync(user.Id);

        // Assert
        habits.Should().HaveCount(2);
        habits.Should().Contain(h => h.Name == "Медитация");
        habits.Should().Contain(h => h.Name == "Зарядка");
    }

    [Fact]
    public async Task HabitRepository_Update_WorksCorrectly()
    {
        // Arrange
        var user = new User
        {
            TelegramUserId = 333444,
            TelegramChatId = 333444,
            Name = "Update Tester",
            RegisteredAt = DateTime.UtcNow
        };
        await _userRepository.AddAsync(user);

        var habit = new Habit
        {
            UserId = user.Id,
            Name = "Старое название",
            ReminderTime = "10:00"
        };
        await _habitRepository.AddAsync(habit);
        await _context.SaveChangesAsync();

        // Act
        habit.Name = "Новое название";
        habit.ReminderTime = "11:00";
        await _habitRepository.UpdateAsync(habit);
        await _context.SaveChangesAsync();

        var updated = await _habitRepository.GetByIdAsync(habit.Id);

        // Assert
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Новое название");
        updated.ReminderTime.Should().Be("11:00");
    }

    [Fact]
    public async Task HabitRepository_SoftDelete_WorksCorrectly()
    {
        // Arrange
        var user = new User
        {
            TelegramUserId = 555666,
            TelegramChatId = 555666,
            Name = "Delete Tester",
            RegisteredAt = DateTime.UtcNow
        };
        await _userRepository.AddAsync(user);

        var habit = new Habit
        {
            UserId = user.Id,
            Name = "Удаляемая привычка",
            ReminderTime = "12:00"
        };
        await _habitRepository.AddAsync(habit);
        await _context.SaveChangesAsync();

        var habitId = habit.Id;

        // Act
        await _habitRepository.DeleteAsync(habit);
        await _context.SaveChangesAsync();

        // Очищаем Change Tracker для корректной работы глобального фильтра в InMemory DB
        _context.ChangeTracker.Clear();

        var deleted = await _habitRepository.GetByIdAsync(habitId);

        // Assert - не должны найти, т.к. применяется глобальный фильтр IsDeleted
        deleted.Should().BeNull();

        // Проверяем, что запись физически существует, но IsDeleted = true
        var rawHabit = await _context.Habits
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(h => h.Id == habitId);
        rawHabit.Should().NotBeNull();
        rawHabit!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task HabitLogRepository_AddLogs_WorksCorrectly()
    {
        // Arrange
        var user = new User
        {
            TelegramUserId = 777888,
            TelegramChatId = 777888,
            Name = "Log Tester",
            RegisteredAt = DateTime.UtcNow
        };
        await _userRepository.AddAsync(user);

        var habit = new Habit
        {
            UserId = user.Id,
            Name = "Логируемая привычка",
            ReminderTime = "08:00"
        };
        await _habitRepository.AddAsync(habit);
        await _context.SaveChangesAsync();

        var log1 = new HabitLog
        {
            HabitId = habit.Id,
            CompletedAt = DateTime.UtcNow.AddDays(-2)
        };
        var log2 = new HabitLog
        {
            HabitId = habit.Id,
            CompletedAt = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        await _habitLogRepository.AddAsync(log1);
        await _habitLogRepository.AddAsync(log2);
        await _context.SaveChangesAsync();

        var logs = await _habitLogRepository.GetHabitLogsAsync(habit.Id);

        // Assert
        logs.Should().HaveCount(2);
    }

    [Fact]
    public async Task HabitLogRepository_GetLogsByDateRange_WorksCorrectly()
    {
        // Arrange
        var user = new User
        {
            TelegramUserId = 999111,
            TelegramChatId = 999111,
            Name = "Range Tester",
            RegisteredAt = DateTime.UtcNow
        };
        await _userRepository.AddAsync(user);

        var habit = new Habit
        {
            UserId = user.Id,
            Name = "Привычка с диапазоном",
            ReminderTime = "09:00"
        };
        await _habitRepository.AddAsync(habit);
        await _context.SaveChangesAsync();

        // Создаем логи за разные дни
        var baseDate = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);
        for (int i = 0; i < 10; i++)
        {
            await _habitLogRepository.AddAsync(new HabitLog
            {
                HabitId = habit.Id,
                CompletedAt = baseDate.AddDays(i)
            });
        }
        await _context.SaveChangesAsync();

        // Act - получаем логи за 5 дней (дни 3-7)
        var fromDate = baseDate.AddDays(3);
        var toDate = baseDate.AddDays(7);
        var logs = await _habitLogRepository.GetHabitLogsByDateRangeAsync(habit.Id, fromDate, toDate);

        // Assert
        logs.Should().HaveCount(5);
        logs.Should().AllSatisfy(log =>
        {
            log.CompletedAt.Should().BeOnOrAfter(fromDate);
            log.CompletedAt.Should().BeOnOrBefore(toDate);
        });
    }

    [Fact]
    public async Task GetByIdWithLogsAsync_IncludesLogs_WorksCorrectly()
    {
        // Arrange
        var user = new User
        {
            TelegramUserId = 222333,
            TelegramChatId = 222333,
            Name = "Include Tester",
            RegisteredAt = DateTime.UtcNow
        };
        await _userRepository.AddAsync(user);

        var habit = new Habit
        {
            UserId = user.Id,
            Name = "Привычка с включенными логами",
            ReminderTime = "10:00"
        };
        await _habitRepository.AddAsync(habit);
        await _context.SaveChangesAsync();

        // Добавляем несколько логов
        for (int i = 0; i < 3; i++)
        {
            await _habitLogRepository.AddAsync(new HabitLog
            {
                HabitId = habit.Id,
                CompletedAt = DateTime.UtcNow.AddDays(-i)
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var habitWithLogs = await _habitRepository.GetByIdWithLogsAsync(habit.Id);

        // Assert
        habitWithLogs.Should().NotBeNull();
        habitWithLogs!.Logs.Should().HaveCount(3);
    }

    [Fact]
    public async Task HabitLogRepository_IsCompletedOnDate_WorksCorrectly()
    {
        // Arrange
        var user = new User
        {
            TelegramUserId = 444555,
            TelegramChatId = 444555,
            Name = "Date Check Tester",
            RegisteredAt = DateTime.UtcNow
        };
        await _userRepository.AddAsync(user);

        var habit = new Habit
        {
            UserId = user.Id,
            Name = "Проверяемая привычка",
            ReminderTime = "09:00"
        };
        await _habitRepository.AddAsync(habit);
        await _context.SaveChangesAsync();

        var testDate = DateTime.UtcNow.Date;
        await _habitLogRepository.AddAsync(new HabitLog
        {
            HabitId = habit.Id,
            CompletedAt = testDate.AddHours(10)
        });
        await _context.SaveChangesAsync();

        // Act
        var isCompleted = await _habitLogRepository.IsCompletedOnDateAsync(habit.Id, testDate);
        var isNotCompleted = await _habitLogRepository.IsCompletedOnDateAsync(habit.Id, testDate.AddDays(1));

        // Assert
        isCompleted.Should().BeTrue();
        isNotCompleted.Should().BeFalse();
    }
}
